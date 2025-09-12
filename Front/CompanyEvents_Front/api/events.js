
const eventsAPI = {
    async getAllEvents(upcomingOnly = true, companyId = null) {
        let url = `${API_BASE_URL}/events?upcomingOnly=${upcomingOnly}`;
        if (companyId) {
            url += `&companyId=${companyId}`;
        }

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Ошибка загрузки событий');
        }
        
        return await response.json();
    },

    async getEventById(id) {
        const response = await fetch(`${API_BASE_URL}/events/${id}`);
        if (!response.ok) {
            throw new Error('Событие не найдено');
        }
        
        return await response.json();
    },

    async getUserEvents() {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/events/my-events`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка загрузки ваших событий');
        }
        
        return await response.json();
    },

    async registerForEvent(eventId) {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/events/${eventId}/register`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка записи на событие');
        }
        
        return await response.json();
    },

    async unregisterFromEvent(eventId) {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/events/${eventId}/register`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка отмены записи');
        }
        
        return await response.json();
    },

    async checkRegistrationDeadline(eventId) {
        const response = await fetch(`${API_BASE_URL}/events/${eventId}/deadline`);
        return await response.json();
    },

    async getEventParticipants(eventId) {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`${API_BASE_URL}/events/${eventId}/participants`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Ошибка загрузки участников');
        }
        
        return await response.json();
    }
};