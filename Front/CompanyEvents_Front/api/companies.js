
const companiesAPI = {
    async getAllCompanies() {
        const response = await fetch(`${API_BASE_URL}/companies`);
        if (!response.ok) {
            throw new Error('Ошибка загрузки компаний');
        }
        
        return await response.json();
    },

    async getCompanyById(id) {
        const response = await fetch(`${API_BASE_URL}/companies/${id}`);
        if (!response.ok) {
            throw new Error('Компания не найдена');
        }
        
        return await response.json();
    },

    async getCompanyEvents(companyId, upcomingOnly = true) {
        const response = await fetch(`${API_BASE_URL}/companies/${companyId}/events?upcomingOnly=${upcomingOnly}`);
        if (!response.ok) {
            throw new Error('Ошибка загрузки событий компании');
        }
        
        return await response.json();
    },

    async createCompany(companyData) {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/companies`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(companyData)
        });
        
        if (!response.ok) {
            throw new Error('Ошибка создания компании');
        }
        
        return await response.json();
    },

    async addManagerToCompany(companyId, managerId) {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/companies/${companyId}/managers/${managerId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка добавления менеджера');
        }
        
        return await response.json();
    }
};