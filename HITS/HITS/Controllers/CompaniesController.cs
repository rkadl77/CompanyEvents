using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            var companies = await _companyService.GetAllCompaniesAsync();
            return Ok(companies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompany(Guid id)
        {
            var company = await _companyService.GetCompanyByIdAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            return Ok(company);
        }

        [HttpPost]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<ActionResult<Company>> CreateCompany(CreateCompanyDto createCompanyDto)
        {
            var managerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId))
            {
                return Unauthorized();
            }

            var company = new Company
            {
                Name = createCompanyDto.Name,
                Description = createCompanyDto.Description
            };

            var createdCompany = await _companyService.CreateCompanyAsync(company, managerId);
            return CreatedAtAction(nameof(GetCompany), new { id = createdCompany.Id }, createdCompany);
        }

        [HttpPost("{companyId}/managers/{managerId}")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<IActionResult> AddManagerToCompany(Guid companyId, string managerId)
        {
            var result = await _companyService.AddManagerToCompanyAsync(companyId, managerId);

            if (!result)
            {
                return BadRequest(new { message = "Failed to add manager to company" });
            }

            return Ok(new { message = "Manager added to company successfully" });
        }
    }
}