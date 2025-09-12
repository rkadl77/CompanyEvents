const API_BASE_URL = 'https://localhost:7166/api';
let currentUser = null;

async function initializeApp() {
  try {
    console.log('Загрузка навбара...');
    const navbarResponse = await fetch('components/navbar.html');
    const navbarHtml = await navbarResponse.text();
    document.getElementById('navbar-container').innerHTML = navbarHtml;
    console.log('Навбар загружен');

    await checkAuth();

    setupRouting();

    loadPage(window.location.hash);
    
  } catch (error) {
    console.error('Ошибка инициализации:', error);

    document.getElementById('app').innerHTML = `
      <div class="notification is-danger">
        <p>Ошибка загрузки приложения. Перезагрузите страницу.</p>
      </div>
    `;
  }
}

document.addEventListener('DOMContentLoaded', function() {
  checkAuth(); 
  setupRouting(); 
  loadPage(window.location.hash);
});

function setupRouting() {
  window.addEventListener('hashchange', () => {
    loadPage(window.location.hash);
  });
}
async function makeRequest(url, options = {}) {
  console.log('=== makeRequest called ===');
  console.log('URL:', `${API_BASE_URL}${url}`);
  
  const token = localStorage.getItem('authToken');
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(`${API_BASE_URL}${url}`, {
      ...options,
      headers
    });

    console.log('Статус ответа:', response.status); 
    console.log('Заголовки ответа:', response.headers); 

    if (response.status === 401) {
      logout();
      throw new Error('Требуется авторизация');
    }

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      const data = await response.json();
      console.log('JSON ответ:', data); 
      return data;
    } else {
      const text = await response.text();
      console.log('Текстовый ответ:', text); 
      return text;
    }
  } catch (error) {
    console.error('Полная ошибка запроса:', error); 
    showNotification('Ошибка соединения с сервером', 'is-danger');
    throw error;
  }
}

function loadPage(hash) {
  console.log('=== loadPage called ===');
  console.log('Hash:', hash);
  
  const appContainer = document.getElementById('app');
  let page = hash.replace('#', '').replace(/\//g, ''); 
  
  console.log('Cleaned page:', page);

  if (page.startsWith('event-detail')) {
    page = 'event-detail';
  } else if (page.startsWith('admin')) {
    page = 'admin';
  }

  if (page === '') page = 'events';

  console.log('Loading page:', page);
  
  fetch(`pages/${page}.html`)
    .then(response => {
      console.log('Page response status:', response.status);
      if (!response.ok) {
        throw new Error('Page not found: ' + response.status);
      }
      return response.text();
    })
    .then(html => {
      console.log('Page loaded successfully');
      appContainer.innerHTML = html;

      if (hash.includes('event-detail')) {
        initPage(page, hash);
      } else {
        initPage(page);
      }
    })
    .catch(err => {
      console.error('Page load error:', err);
      appContainer.innerHTML = '<p>Страница не найдена.</p>';
    });
}

function initPage(pageName, hash = '') {
  console.log('=== initPage called ===');
  console.log('Page name:', pageName, 'Hash:', hash);

  const cleanPageName = pageName.replace(/\//g, '');
  console.log('Clean page name:', cleanPageName);
  
  switch(cleanPageName) {
    case 'login':
      console.log('Initializing login page');
      initLoginPage();
      break;
    case 'register':
      console.log('Initializing register page');
      initRegisterPage();
      break;
    case 'events':
      console.log('Initializing events page');
      initEventsPage();
      break;
    case 'event-detail':
      console.log('Initializing event detail page with hash:', hash);
      initEventDetailPage(hash);
      break;
    case 'profile':
      console.log('Initializing profile page');
      initProfilePage();
      break;
    case 'admin':
      console.log('Initializing admin page');
      initAdminPage();
      break;
    default:
      console.log('Unknown page:', cleanPageName);
  }
}

function initProfileEditPage() {
  if (!currentUser || !currentUser.isApproved) {
    showNotification('Для редактирования профиля необходимо быть подтвержденным пользователем', 'is-warning');
    window.location.hash = '#profile';
    return;
  }

  document.getElementById('edit-firstName').value = currentUser.firstName;
  document.getElementById('edit-lastName').value = currentUser.lastName;
  document.getElementById('edit-email').value = currentUser.email;

  const form = document.getElementById('profile-edit-form');
  if (form) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      
      const formData = {
        firstName: document.getElementById('edit-firstName').value,
        lastName: document.getElementById('edit-lastName').value,
        email: document.getElementById('edit-email').value
      };

      const password = document.getElementById('edit-password').value;
      if (password) {
        formData.password = password;
      }

      try {
        const data = await makeRequest('/users/profile', {
          method: 'PUT',
          body: JSON.stringify(formData)
        });
        
        showNotification('Профиль успешно обновлен!', 'is-success');
        currentUser = { ...currentUser, ...formData };
        updateUIForAuthUser(currentUser);
        
        setTimeout(() => {
          window.location.hash = '#profile';
        }, 1000);
      } catch (error) {
        showNotification('Ошибка обновления профиля: ' + error.message, 'is-danger');
      }
    });
  }
}

async function makeRequest(url, options = {}) {
  console.log('=== makeRequest called ===');
  console.log('URL:', `${API_BASE_URL}${url}`);
  
  const token = localStorage.getItem('authToken');
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(`${API_BASE_URL}${url}`, {
      ...options,
      headers,
    });

    console.log('Response status:', response.status);
    console.log('Response headers:', Object.fromEntries(response.headers.entries()));

    if (response.status === 401) {
      logout();
      throw new Error('Требуется авторизация');
    }

    const responseText = await response.text();
    console.log('Response text:', responseText.substring(0, 200) + '...'); 


    try {
      const data = JSON.parse(responseText);
      console.log('JSON parsed successfully:', data);
      return data;
    } catch (jsonError) {
      console.warn('Response is not JSON, returning text');

      throw new Error(`Server error: ${response.status} - ${responseText.substring(0, 100)}`);
    }

  } catch (error) {
    console.error('Request error:', error);
    showNotification('Ошибка сервера: ' + error.message, 'is-danger');
    throw error;
  }
}

async function checkAuth() {
  console.log('Проверка авторизации...');
  const token = localStorage.getItem('authToken');
  
  if (token) {
    try {
      console.log('Токен найден, проверяем пользователя...');
      const userData = await makeRequest('/auth/me');
      currentUser = userData;
      console.log('Пользователь получен:', userData);
      updateUIForAuthUser(userData);
    } catch (error) {
      console.log('Ошибка проверки токена, разлогиниваемся');
      logout();
    }
  } else {
    console.log('Токен не найден, гость');
    updateUIForGuest();
  }
}

function updateUIForAuthUser(user) {
  const elements = {
    userInfo: document.getElementById('user-info'),
    authButtons: document.getElementById('auth-buttons'),
    navProfile: document.getElementById('nav-profile'),
    navAdmin: document.getElementById('nav-admin'),
    userName: document.getElementById('user-name')
  };
  
  if (elements.userInfo) elements.userInfo.classList.remove('is-hidden');
  if (elements.authButtons) elements.authButtons.classList.add('is-hidden');
  if (elements.navProfile) elements.navProfile.classList.remove('is-hidden');
  
  if (elements.userName) {
    elements.userName.textContent = `${user.firstName} ${user.lastName}`;
    
    if (user.role === 'Deanery') {
      elements.userName.innerHTML += ' <span class="tag is-danger">Администратор</span>';
    } else if (!user.isApproved) {
      elements.userName.innerHTML += ' <span class="tag is-warning">Ожидает подтверждения</span>';
    } else {
      elements.userName.innerHTML += ' <span class="tag is-success">Подтвержден</span>';
    }
  }

  if (user.role === 'Deanery' && elements.navAdmin) {
    elements.navAdmin.classList.remove('is-hidden');
  }
}

function updateUIForGuest() {
  console.log('Обновление UI для гостя');
  
  const elements = {
    userInfo: document.getElementById('user-info'),
    authButtons: document.getElementById('auth-buttons'),
    navProfile: document.getElementById('nav-profile'),
    navAdmin: document.getElementById('nav-admin')
  };

  if (elements.userInfo) elements.userInfo.classList.add('is-hidden');
  if (elements.authButtons) elements.authButtons.classList.remove('is-hidden');
  if (elements.navProfile) elements.navProfile.classList.add('is-hidden');
  if (elements.navAdmin) elements.navAdmin.classList.add('is-hidden');
  
  console.log('UI обновлен для гостя');
}

function logout() {
  localStorage.removeItem('authToken');
  currentUser = null;
  updateUIForGuest();
  window.location.hash = '#events';
}

function showNotification(message, type = 'is-info') {
  const notification = document.createElement('div');
  notification.className = `notification ${type}`;
  notification.innerHTML = `
    <button class="delete"></button>
    ${message}
  `;
  
  document.body.appendChild(notification);
  
  notification.querySelector('.delete').addEventListener('click', () => {
    notification.remove();
  });
  
  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, 3000);
}

function initLoginPage() {
  console.log('=== initLoginPage called ==='); 
  
  const form = document.getElementById('login-form');
  console.log('Form element:', form); 
  
  if (form) {
    console.log('Login form found, adding event listener'); 
    
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      console.log('Login form submitted!'); 
      
      const email = document.getElementById('email').value;
      const password = document.getElementById('password').value;
      
      console.log('Credentials:', { email, password }); 
      
      try {
        console.log('Making request to /auth/login...'); 
        const data = await makeRequest('/auth/login', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        });
        
        console.log('Login response:', data); 
        
        localStorage.setItem('authToken', data.token);
        currentUser = data.user;
        updateUIForAuthUser(data.user);
        showNotification('Успешный вход!', 'is-success');
        window.location.hash = '#events';
      } catch (error) {
        console.error('Login error:', error); 
        showNotification('Ошибка входа: ' + error.message, 'is-danger');
      }
    });
  } else {
    console.error('Login form NOT found!'); 
    console.log('Available elements:', document.querySelectorAll('form')); 
  }
}

function initRegisterPage() {
    console.log('=== initRegisterPage called ===');
  console.log('Current URL hash:', window.location.hash);
  
  setTimeout(() => {
    let form = document.getElementById('register-form');
    console.log('Form found after timeout:', form);
    
    // Если форма не найдена - создаем ее
    if (!form) {
      console.log('Creating fallback register form...');
      const appContainer = document.getElementById('app');
      appContainer.innerHTML = `
        <div class="columns is-centered">
          <div class="column is-half">
            <div class="card">
              <header class="card-header">
                <p class="card-header-title">Регистрация</p>
              </header>
              <div class="card-content">
                <form id="register-form">
                  <div class="field">
                    <label class="label">Имя</label>
                    <div class="control">
                      <input class="input" type="text" id="firstName" placeholder="Ваше имя" required>
                    </div>
                  </div>
                  <div class="field">
                    <label class="label">Фамилия</label>
                    <div class="control">
                      <input class="input" type="text" id="lastName" placeholder="Ваша фамилия" required>
                    </div>
                  </div>
                  <div class="field">
                    <label class="label">Email</label>
                    <div class="control">
                      <input class="input" type="email" id="email" placeholder="Ваш email" required>
                    </div>
                  </div>
                  <div class="field">
                    <label class="label">Пароль</label>
                    <div class="control">
                      <input class="input" type="password" id="password" placeholder="Пароль" required>
                    </div>
                  </div>
                  <div class="field">
                    <label class="label">Роль</label>
                    <div class="control">
                      <select class="input" id="role" required>
                        <option value="">Выберите роль</option>
                        <option value="Student">Студент</option>
                      </select>
                    </div>
                  </div>
                  <div class="field">
                    <div class="control">
                      <button type="submit" class="button is-primary is-fullwidth">Зарегистрироваться</button>
                    </div>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      `;
      form = document.getElementById('register-form');
    }
    
    if (form) {
      console.log('Register form found, adding event listener');
      
      form.addEventListener('submit', async (e) => {
        e.preventDefault();
        console.log('Register form submitted!');
        
        const formData = {
          firstName: document.getElementById('firstName').value,
          lastName: document.getElementById('lastName').value,
          email: document.getElementById('email').value,
          password: document.getElementById('password').value,
          role: document.getElementById('role').value
        };
        
        console.log('Form data:', formData);
        
        try {
          console.log('Making request to /auth/register...');
          const data = await makeRequest('/auth/register', {
            method: 'POST',
            body: JSON.stringify(formData)
          });
          
          console.log('Register response:', data);
          
          showNotification(data.message || 'Регистрация успешна! Ожидайте подтверждения.', 'is-success');
          setTimeout(() => {
            window.location.hash = '#login';
          }, 2000);
        } catch (error) {
          console.error('Register error:', error);
          showNotification('Ошибка регистрации: ' + error.message, 'is-danger');
        }
      });
    }
  }, 100);
}

function initEventsPage() {
  console.log('=== initEventsPage called ===');

  setTimeout(() => {
    console.log('Loading events...');
    loadEvents();
    setupEventFilters();
  }, 100);
}

async function loadEvents(filters = {}) {
  console.log('=== loadEvents called ===');
  
  try {
    let url = '/events?upcomingOnly=true';
    if (filters.companyId) {
      url += `&companyId=${filters.companyId}`;
    }
    
    console.log('Fetching events from:', url);
    
    const events = await makeRequest(url);
    console.log('Events received:', events);
    
    displayEvents(events);
    loadCompaniesForFilter();
  } catch (error) {
    console.error('Ошибка загрузки событий:', error);

    const container = document.getElementById('events-container');

    if (container) {
      container.innerHTML = `
        <div class="notification is-danger">
          <p>Ошибка загрузки мероприятий: ${error.message}</p>
        </div>
      `;
    }
  }
}

function displayEvents(events) {
  const container = document.getElementById('events-container');
  if (!container) return;
  
  const isDeanery = currentUser && currentUser.role === 'Deanery';
  const isApprovedStudent = currentUser && currentUser.isApproved && currentUser.role === 'Student';
  const isApprovedManager = currentUser && currentUser.isApproved && currentUser.role === 'CompanyManager';
  
  if (!currentUser) {
    container.innerHTML = `
      <div class="notification is-warning">
        <p>Для просмотра мероприятий необходимо <a href="#/login">войти</a> или <a href="#/register">зарегистрироваться</a></p>
      </div>
    `;
    return;
  }
  
  if (!isDeanery && !currentUser.isApproved) {
    container.innerHTML = `
      <div class="notification is-info">
        <p>Ваш аккаунт ожидает подтверждения администратором</p>
        <p>После подтверждения вы сможете просматривать мероприятия</p>
      </div>
    `;
    return;
  }
  
  if (events.length === 0) {
    container.innerHTML = '<p class="has-text-centered">Нет доступных мероприятий</p>';
    return;
  }
  
  container.innerHTML = events.map(event => `
    <div class="card mb-4 event-card" data-event-id="${event.id}">
      <header class="card-header">
        <p class="card-header-title">${event.title}</p>
      </header>
      <div class="card-content">
        <div class="content">
          ${event.description ? `<p>${event.description}</p>` : ''}
          <p><strong>📅 Дата:</strong> ${new Date(event.date).toLocaleString('ru-RU')}</p>
          <p><strong>📍 Место:</strong> ${event.location}</p>
          <p><strong>🏢 Организатор:</strong> ${event.companyName}</p>
          ${event.registrationDeadline ? 
            `<p><strong>⏰ Дедлайн записи:</strong> ${new Date(event.registrationDeadline).toLocaleString('ru-RU')}</p>` : ''}
        </div>
      </div>
      <footer class="card-footer">
        <a href="#/event-detail?id=${event.id}" class="card-footer-item">Подробнее</a>
        ${isApprovedStudent ? 
          `<button class="card-footer-item register-btn" data-event-id="${event.id}">Записаться</button>` : ''}
      </footer>
    </div>
  `).join('');
}

async function loadCompaniesForFilter() {
  try {
    const companies = await makeRequest('/companies');
    const filterSelect = document.getElementById('company-filter');
    if (filterSelect) {
      filterSelect.innerHTML = '<option value="">Все компании</option>' +
        companies.map(company => 
          `<option value="${company.id}">${company.name}</option>`
        ).join('');
      
      filterSelect.addEventListener('change', (e) => {
        loadEvents({ companyId: e.target.value });
      });
    }
  } catch (error) {
    console.error('Ошибка загрузки компаний:', error);
  }
}

function setupEventFilters() {
  const searchInput = document.getElementById('event-search');
  if (searchInput) {
    searchInput.addEventListener('input', debounce((e) => {
      filterEvents(e.target.value);
    }, 300));
  }
}

function filterEvents(searchText) {
  const cards = document.querySelectorAll('.event-card');
  cards.forEach(card => {
    const title = card.querySelector('.card-header-title').textContent.toLowerCase();
    const description = card.querySelector('.content').textContent.toLowerCase();
    const searchLower = searchText.toLowerCase();
    
    if (title.includes(searchLower) || description.includes(searchLower)) {
      card.style.display = 'block';
    } else {
      card.style.display = 'none';
    }
  });
}

function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

async function registerForEvent(eventId) {
  console.log('=== registerForEvent called ===');
  console.log('Event ID:', eventId);
  
  if (!currentUser || !currentUser.isApproved) {
    showNotification('Ваш аккаунт не подтвержден', 'is-warning');
    return;
  }

  if (currentUser.role !== 'Student') {
    showNotification('Только студенты могут записываться', 'is-warning');
    return;
  }

  try {
    console.log('Making registration request...');
    const result = await makeRequest(`/events/${eventId}/register`, {
      method: 'POST'
    });
    
    console.log('Registration successful:', result);
    showNotification('Вы успешно записались на мероприятие!', 'is-success');
    
    const btn = document.querySelector(`[data-event-id="${eventId}"]`);
    if (btn) {
      btn.textContent = 'Записан';
      btn.classList.add('has-text-success');
      btn.disabled = true;
    }
  } catch (error) {
    console.error('Registration error:', error);
    
    if (error.message.includes('already registered')) {
      showNotification('Вы уже записаны на это мероприятие', 'is-warning');
    } else if (error.message.includes('deadline')) {
      showNotification('Дедлайн записи прошел', 'is-warning');
    } else if (error.message.includes('System.Exception')) {
      showNotification('Ошибка сервера при записи', 'is-danger');
    } else {
      showNotification('Ошибка записи: ' + error.message, 'is-danger');
    }
  }
}

function initEventDetailPage(hash) {
  console.log('=== initEventDetailPage called with hash:', hash);

  let eventId = '';
  if (hash) {
    const match = hash.match(/id=([a-f0-9-]+)/);
    if (match) {
      eventId = match[1];
    }
  }
  
  console.log('Extracted event ID:', eventId);
  
  if (eventId) {
    loadEventDetail(eventId);
  } else {
    const container = document.getElementById('event-detail-container');
    if (container) {
      container.innerHTML = '<p>Событие не найдено</p>';
    }
  }
}

async function loadEventDetail(eventId) {
  try {
    console.log('Loading event details for ID:', eventId);
    const event = await makeRequest(`/events/${eventId}`);
    const container = document.getElementById('event-detail-container');
    
    if (!container) {
      console.error('Event detail container not found');
      return;
    }
    
    const isDeanery = currentUser && currentUser.role === 'Deanery';
    const isApprovedStudent = currentUser && currentUser.isApproved && currentUser.role === 'Student';
    const isApprovedManager = currentUser && currentUser.isApproved && currentUser.role === 'CompanyManager';
    
    let actionButtons = '';
    
    // Кнопки для студентов
    if (isApprovedStudent) {
      actionButtons = `<button class="button is-primary register-btn-detail">Записаться</button>`;
    }
    
    // Кнопка "Участники" для деканата и менеджеров
    if (isDeanery || (isApprovedManager && event.companyId === currentUser.companyId)) {
      actionButtons += `<button class="button is-info view-participants-btn">Участники</button>`;
    }
    
    container.innerHTML = `
      <div class="card">
        <header class="card-header">
          <p class="card-header-title is-size-3">${event.title}</p>
        </header>
        <div class="card-content">
          <div class="content">
            ${event.description ? `<p class="is-size-5">${event.description}</p><hr>` : ''}
            <p><strong>📅 Дата и время:</strong> ${new Date(event.date).toLocaleString('ru-RU')}</p>
            <p><strong>📍 Место:</strong> ${event.location}</p>
            <p><strong>🏢 Организатор:</strong> ${event.companyName}</p>
            ${event.registrationDeadline ? 
              `<p><strong>⏰ Дедлайн записи:</strong> ${new Date(event.registrationDeadline).toLocaleString('ru-RU')}</p>` : ''}
            <p><strong>👥 Участников:</strong> ${event.participants ? event.participants.length : 0}</p>
          </div>
        </div>
        ${actionButtons ? `
        <footer class="card-footer">
          ${actionButtons}
        </footer>
        ` : ''}
      </div>
    `;
    
    // Обработчики кнопок
    const registerBtn = document.querySelector('.register-btn-detail');
    if (registerBtn) {
      registerBtn.addEventListener('click', () => registerForEvent(eventId));
    }
    
    const viewParticipantsBtn = document.querySelector('.view-participants-btn');
    if (viewParticipantsBtn) {
      viewParticipantsBtn.addEventListener('click', () => {
        window.location.hash = `#/admin?view=participants&eventId=${eventId}`;
      });
    }
    
  } catch (error) {
    console.error('Ошибка загрузки деталей события:', error);
    const container = document.getElementById('event-detail-container');
    if (container) {
      container.innerHTML = `
        <div class="notification is-danger">
          <p>Ошибка загрузки события: ${error.message}</p>
        </div>
      `;
    }
  }
}

function initProfilePage() {
  if (!currentUser) {
    window.location.hash = '#login';
    return;
  }
  
  loadUserEvents();
  setupGoogleCalendarIntegration();
}

async function loadUserEvents() {
  try {
    const events = await makeRequest('/events/my-events');
    const container = document.getElementById('user-events-container');
    
    const isDeanery = currentUser && currentUser.role === 'Deanery';

    if (isDeanery) {
      container.innerHTML = '<p>У администраторов нет личных мероприятий</p>';
      return;
    }

    if (!currentUser.isApproved) {
      container.innerHTML = `
        <div class="notification is-info">
          <p>Ваш аккаунт ожидает подтверждения администратором</p>
          <p>После подтверждения вы увидите здесь свои мероприятия</p>
        </div>
      `;
      return;
    }
    
    if (events.length === 0) {
      container.innerHTML = '<p>Вы еще не записаны ни на одно мероприятие</p>';
      return;
    }
    
    container.innerHTML = events.map(event => `
      <div class="card mb-3">
        <div class="card-content">
          <div class="content">
            <h4 class="title is-5">${event.title}</h4>
            <p><strong>Дата:</strong> ${new Date(event.date).toLocaleString('ru-RU')}</p>
            <p><strong>Место:</strong> ${event.location}</p>
            <p><strong>Организатор:</strong> ${event.companyName}</p>
          </div>
        </div>
        <footer class="card-footer">
          <a href="#/event-detail?id=${event.id}" class="card-footer-item">Подробнее</a>
          ${currentUser.role === 'Student' ? 
            `<button class="card-footer-item button is-danger unregister-btn" data-event-id="${event.id}">
              Отменить запись
            </button>` : ''}
        </footer>
      </div>
    `).join('');
  } catch (error) {
    console.error('Ошибка загрузки событий пользователя:', error);
  }
}

async function unregisterFromEvent(eventId) {
  if (!confirm('Вы уверены, что хотите отменить запись на мероприятие?')) {
    return;
  }
  
  try {
    await makeRequest(`/events/${eventId}/register`, {
      method: 'DELETE'
    });
    
    showNotification('Запись отменена', 'is-info');
    loadUserEvents(); 
  } catch (error) {
    console.error('Ошибка отмены записи:', error);
  }
}

function setupGoogleCalendarIntegration() {
  const connectBtn = document.getElementById('connect-google-btn');
  const statusDiv = document.getElementById('google-status');
  
  if (connectBtn) {
    connectBtn.addEventListener('click', async () => {
      try {
        const response = await makeRequest('/googleauth/auth-url');
        window.location.href = response.authUrl;
      } catch (error) {
        console.error('Ошибка подключения Google Calendar:', error);
      }
    });
  }

  checkGoogleCalendarStatus();
}

async function checkGoogleCalendarStatus() {
  try {
    const status = await makeRequest('/googleauth/status');
    const statusDiv = document.getElementById('google-status');
    if (statusDiv) {
      statusDiv.innerHTML = status.hasAccess ? 
        '<span class="has-text-success">✅ Календарь подключен</span>' :
        '<span class="has-text-danger">❌ Календарь не подключен</span>';
    }
  } catch (error) {
    console.error('Ошибка проверки статуса Google Calendar:', error);
  }
}

function initAdminPage() {
  console.log('=== initAdminPage called ===');
  
  if (!currentUser || currentUser.role !== 'Deanery') {
    showNotification('Доступ только для администраторов', 'is-warning');
    window.location.hash = '#events';
    return;
  }
  
  console.log('User is admin, loading data...');
  loadPendingUsers();
  loadAllCompanies();
  setupAdminTabs();
  
  const urlParams = new URLSearchParams(window.location.search);
  const view = urlParams.get('view');
  const eventId = urlParams.get('eventId');
  
  if (view === 'participants' && eventId) {
    console.log('Loading participants for event:', eventId);
  }
}

async function loadAllCompanies() {
  console.log('=== loadAllCompanies called ==='); // ← ДОБАВЬ
  
  try {
    console.log('Making request to /companies...'); // ← ДОБАВЬ
    const companies = await makeRequest('/companies');
    console.log('Companies response:', companies); // ← ДОБАВЬ
    
    const container = document.getElementById('companies-container');
    if (!container) {
      console.error('Companies container not found!'); // ← ДОБАВЬ
      return;
    }
    
    if (!companies || companies.length === 0) {
      container.innerHTML = '<p>Нет созданных компаний</p>';
      return;
    }
    
    container.innerHTML = companies.map(company => `
      <div class="card mb-3">
        <div class="card-content">
          <div class="content">
            <h4 class="title is-5">${company.name}</h4>
            ${company.description ? `<p>${company.description}</p>` : ''}
            <p><strong>ID:</strong> ${company.id}</p>
            <p><strong>Менеджеров:</strong> ${company.managers ? company.managers.length : 0}</p>
          </div>
        </div>
        <footer class="card-footer">
          <button class="card-footer-item button is-info view-managers-btn" data-company-id="${company.id}">
            Менеджеры
          </button>
          <button class="card-footer-item button is-warning add-manager-btn" data-company-id="${company.id}">
            Добавить менеджера
          </button>
        </footer>
      </div>
    `).join('');

    // Добавим проверку что кнопки существуют
    const viewButtons = document.querySelectorAll('.view-managers-btn');
    console.log('View managers buttons found:', viewButtons.length); // ← ДОБАВЬ
    
    viewButtons.forEach(btn => {
      btn.addEventListener('click', (e) => {
        const companyId = e.target.dataset.companyId;
        console.log('View managers for company:', companyId); // ← ДОБАВЬ
        viewCompanyManagers(companyId);
      });
    });

    const addButtons = document.querySelectorAll('.add-manager-btn');
    console.log('Add manager buttons found:', addButtons.length); // ← ДОБАВЬ
    
    addButtons.forEach(btn => {
      btn.addEventListener('click', (e) => {
        const companyId = e.target.dataset.companyId;
        console.log('Add manager to company:', companyId); // ← ДОБАВЬ
        showAddManagerForm(companyId);
      });
    });

  } catch (error) {
    console.error('Ошибка загрузки компаний:', error);
    const container = document.getElementById('companies-container');
    if (container) {
      container.innerHTML = `
        <div class="notification is-danger">
          <p>Ошибка загрузки компаний: ${error.message}</p>
          <p>Проверьте консоль для подробностей</p>
        </div>
      `;
    }
  }
}

function showAddManagerForm(companyId) {
  const managerEmail = prompt('Введите email менеджера:');
  if (managerEmail) {
    addManagerToCompany(companyId, managerEmail);
  }
}

async function addManagerToCompany(companyId, managerEmail) {
  try {
    const users = await makeRequest('/users/search?email=' + encodeURIComponent(managerEmail));
    if (users.length === 0) {
      showNotification('Пользователь с таким email не найден', 'is-warning');
      return;
    }

    const managerId = users[0].id;
    const result = await makeRequest(`/companies/${companyId}/managers/${managerId}`, {
      method: 'POST'
    });
    
    showNotification('Менеджер успешно добавлен в компанию!', 'is-success');
    loadAllCompanies(); 
  } catch (error) {
    showNotification('Ошибка добавления менеджера: ' + error.message, 'is-danger');
  }
}

async function loadAllUsers() {
  try {
    const users = await makeRequest('/users/all');
    const container = document.getElementById('all-users-container');
    
    container.innerHTML = users.map(user => `
      <div class="card mb-3">
        <div class="card-content">
          <div class="content">
            <h4 class="title is-5">${user.firstName} ${user.lastName}</h4>
            <p><strong>Email:</strong> ${user.email}</p>
            <p><strong>Роль:</strong> ${user.role}</p>
            <p><strong>Статус:</strong> 
              <span class="tag ${user.isApproved ? 'is-success' : 'is-warning'}">
                ${user.isApproved ? 'Подтвержден' : 'Ожидает'}
              </span>
            </p>
            ${user.companyId ? `<p><strong>Компания:</strong> ${user.companyId}</p>` : ''}
          </div>
        </div>
      </div>
    `).join('');
  } catch (error) {
    console.error('Ошибка загрузки пользователей:', error);
  }
}

async function loadPendingUsers() {
  console.log('=== loadPendingUsers called ==='); 
  
  try {
    console.log('Making request to /users/pending...'); 
    const users = await makeRequest('/users/pending');
    console.log('Pending users response:', users); 
    
    const container = document.getElementById('pending-users-container');
    if (!container) {
      console.error('Pending users container not found!'); 
      return;
    }
    
    if (!users || users.length === 0) {
      container.innerHTML = '<p>Нет пользователей на модерации</p>';
      return;
    }
    
    container.innerHTML = users.map(user => `
      <div class="card mb-3">
        <div class="card-content">
          <div class="content">
            <h4 class="title is-5">${user.firstName} ${user.lastName}</h4>
            <p><strong>Email:</strong> ${user.email}</p>
            <p><strong>Роль:</strong> ${user.role}</p>
            ${user.rejectionReason ? `<p><strong>Причина отказа:</strong> ${user.rejectionReason}</p>` : ''}
          </div>
        </div>
        <footer class="card-footer">
          <button class="card-footer-item button is-success approve-btn" data-user-id="${user.id}">
            Подтвердить
          </button>
          <button class="card-footer-item button is-danger reject-btn" data-user-id="${user.id}">
            Отклонить
          </button>
        </footer>
      </div>
    `).join('');

    const approveButtons = document.querySelectorAll('.approve-btn');
    console.log('Approve buttons found:', approveButtons.length); 
    
    approveButtons.forEach(btn => {
      btn.addEventListener('click', (e) => {
        const userId = e.target.dataset.userId;
        console.log('Approving user:', userId); 
        approveUser(userId);
      });
    });
    
    const rejectButtons = document.querySelectorAll('.reject-btn');
    console.log('Reject buttons found:', rejectButtons.length); 
    
    rejectButtons.forEach(btn => {
      btn.addEventListener('click', (e) => {
        const userId = e.target.dataset.userId;
        console.log('Rejecting user:', userId); 
        const reason = prompt('Введите причину отказа:');
        if (reason) {
          rejectUser(userId, reason);
        }
      });
    });
  } catch (error) {
    console.error('Ошибка загрузки пользователей на модерации:', error);
    const container = document.getElementById('pending-users-container');
    if (container) {
      container.innerHTML = `
        <div class="notification is-danger">
          <p>Ошибка загрузки пользователей: ${error.message}</p>
          <p>Проверьте консоль для подробностей</p>
        </div>
      `;
    }
  }
}

async function approveUser(userId) {
  try {
    await makeRequest(`/users/${userId}/approve`, {
      method: 'POST'
    });
    
    showNotification('Пользователь подтвержден', 'is-success');
    loadPendingUsers(); 
  } catch (error) {
    console.error('Ошибка подтверждения пользователя:', error);
  }
}

async function rejectUser(userId, reason) {
  try {
    await makeRequest(`/users/${userId}/reject`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    });
    
    showNotification('Пользователь отклонен', 'is-info');
    loadPendingUsers();
  } catch (error) {
    console.error('Ошибка отклонения пользователя:', error);
  }
}

function setupAdminTabs() {
  const tabs = document.querySelectorAll('.admin-tab');
  tabs.forEach(tab => {
    tab.addEventListener('click', (e) => {
      const tabName = e.target.dataset.tab;

      document.querySelectorAll('.admin-content').forEach(content => {
        content.style.display = 'none';
      });

      document.getElementById(`${tabName}-tab`).style.display = 'block';

      tabs.forEach(t => t.classList.remove('is-active'));
      e.target.classList.add('is-active');
    });
  });
}

document.addEventListener('click', (e) => {
  if (e.target.id === 'logout-btn') {
    logout();
  }
});

if (window.location.hash.includes('google_connected')) {
  showNotification('Google Calendar успешно подключен!', 'is-success');
  window.history.replaceState({}, document.title, window.location.pathname);
}