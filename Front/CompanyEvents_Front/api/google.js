
const googleAPI = {
    async getAuthUrl() {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/googleauth/auth-url`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка получения URL аутентификации');
        }
        
        return await response.json();
    },

    async getCalendarStatus() {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/googleauth/status`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка проверки статуса');
        }
        
        return await response.json();
    },

    async saveTokens(userId, code, redirectUri) {
        const response = await fetch(`${API_BASE_URL}/googleauth/callback?code=${code}&state=${userId}&redirect_uri=${encodeURIComponent(redirectUri)}`);
        return await response.json();
    },

    async addEventToCalendar(eventData) {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/googleauth/test-add-event`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(eventData)
        });
        
        return await response.json();
    }
};