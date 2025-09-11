using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HITS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompaniesController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetCompanies()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                var dtos = companies.Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                });
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CompanyDto>> GetCompany(Guid id)
        {
            try
            {
                var company = await _companyService.GetCompanyByIdAsync(id);
                if (company == null)
                {
                    return NotFound(new { message = "Company not found" });
                }

                var dto = new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Description = company.Description
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}/events")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Event>>> GetCompanyEvents(
            Guid id,
            [FromQuery] bool upcomingOnly = true)
        {
            try
            {
                var events = await _companyService.GetCompanyEventsAsync(id, upcomingOnly);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<ActionResult<CompanyDto>> CreateCompany(CreateCompanyDto createCompanyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var company = new Company
                {
                    Name = createCompanyDto.Name,
                    Description = createCompanyDto.Description
                };

                var createdCompany = await _companyService.CreateCompanyAsync(company, managerId);

                var dto = new CompanyDto
                {
                    Id = createdCompany.Id,
                    Name = createdCompany.Name,
                    Description = createdCompany.Description
                };

                return CreatedAtAction(nameof(GetCompany), new { id = dto.Id }, dto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{companyId}/managers/{managerId}")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<IActionResult> AddManagerToCompany(Guid companyId, string managerId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var result = await _companyService.AddManagerToCompanyAsync(companyId, managerId, currentUserId);

                if (!result)
                {
                    return BadRequest(new { message = "Failed to add manager to company. Check if manager exists and is approved." });
                }

                return Ok(new { message = "Manager added to company successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-company")]
        [Authorize(Roles = "CompanyManager")]
        public async Task<ActionResult<CompanyDto>> GetMyCompany()
        {
            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var company = await _companyService.GetManagerCompanyAsync(managerId);
                if (company == null)
                {
                    return NotFound(new { message = "You are not associated with any company" });
                }

                var dto = new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Description = company.Description
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
        [HttpGet("{id}/managers")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetCompanyManagers(Guid id)
        {
            try
            {
                var company = await _companyService.GetCompanyByIdAsync(id);
                if (company == null)
                {
                    return NotFound(new { message = "Company not found" });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAuthorized = User.IsInRole("Deanery") || company.Managers.Any(m => m.Id == currentUserId);
                if (!isAuthorized)
                {
                    return Forbid();
                }

                var managerDtos = company.Managers.Select(m => new UserDto
                {
                    Id = m.Id,
                    Email = m.Email,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Role = m.Role,
                    IsApproved = m.IsApproved
                });

                return Ok(managerDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
        [HttpDelete("{companyId}/managers/{managerId}")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<IActionResult> RemoveManagerFromCompany(Guid companyId, string managerId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var result = await _companyService.RemoveManagerFromCompanyAsync(companyId, managerId, currentUserId);

                if (!result)
                {
                    return BadRequest(new { message = "Failed to remove manager from company." });
                }

                return Ok(new { message = "Manager removed from company successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
