# 🎓 HITS Events - Система управления мероприятиями

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-6.0-purple.svg)
![License](https://img.shields.io/badge/license-MIT-yellow.svg)

**Платформа для организации мероприятий вуза с интеграцией Google Calendar и Telegram ботом**

---

## ✨ Возможности

### 🎯 Для студентов
- 📅 Просмотр всех мероприятий университета
- ✅ Запись на интересующие события
- 📱 Интеграция с Google Calendar
- 🔔 Уведомления о новых мероприятиях

### 🏢 Для компаний-партнеров
- 🤖 Полный функционал через Telegram-бота
- 📊 Управление мероприятиями (CRUD)
- 👥 Просмотр списков участников
- ⏰ Установка дедлайнов записи

### 🎓 Для деканата
- 👨‍💼 Панель администратора
- ✅ Модерация пользователей
- 🏢 Управление компаниями
- 📊 Аналитика посещаемости

---

## 🛠️ Технологический стек

### Backend
- **ASP.NET Core 8.0** - основной фреймворк
- **Entity Framework Core** - ORM для работы с БД
- **JWT Authentication** - аутентификация
- **MySQL** - система управления базами данных
- **Swagger** - документация API

### Frontend
- **Vanilla JavaScript** - чистый JS без фреймворков
- **Bulma CSS** - современный CSS фреймворк
- **Font Awesome** - иконки
- **Google Calendar API** - интеграция с календарем

### Дополнительно
- **Telegram Bot API** - бот для менеджеров
- **Google OAuth 2.0** - аутентификация Google

---

## 🚀 Быстрый старт

### Предварительные требования
- .NET 6.0 SDK
- MySQL Server 8.0
- Telegram Bot Token
- Google OAuth Credentials

### Установка Backend

```bash
# Клонирование репозитория
git clone https://github.com/your-username/hits-events.git
cd hits-events/HITS

# Настройка базы данных
dotnet ef database update

# Запуск приложения
dotnet run
