import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import './ChatsPage.css';

function ChatsPage() {
    const [searchQuery, setSearchQuery] = useState('');
    const [users, setUsers] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);

    // Функция для поиска пользователей
    const searchUsers = async (query) => {
        if (!query.trim()) {
            setUsers([]);
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const response = await fetch(
                `http://localhost:5078/api/chats/users?search=${encodeURIComponent(query)}`,
                {
                    credentials: 'include',  // Убедимся, что куки отправляются с запросом
                    headers: {
                        'Content-Type': 'application/json',
                        // Мы больше не передаем Bearer токен
                    }
                }
            );

            if (!response.ok) {
                throw new Error(`Ошибка поиска: ${response.status}`);
            }

            const data = await response.json();
            console.log(data);

            
            setUsers(data);
        } catch (err) {
            setError(err.message);
            console.error("Ошибка при поиске пользователей:", err);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        const timer = setTimeout(() => {
            searchUsers(searchQuery).then(response => {
                console.log('Search completed:', response);
            }).catch(error => {
                console.error('Search error:', error);
            });
        }, 300);

        return () => clearTimeout(timer);
    }, [searchQuery]);


    return (
        <div className="chats-container">
            <div className="chats-header">
                <h1>SecureChat</h1>
                <div className="search-container">
                    <input
                        type="text"
                        placeholder="Поиск по имени пользователя"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        disabled={isLoading}
                    />

                    {isLoading && (
                        <div className="search-loading">
                            <div className="spinner"></div>
                            <span>Поиск...</span>
                        </div>
                    )}
                </div>
            </div>

            {error && (
                <div className="error-message">
                    {error}
                    <button onClick={() => setError(null)}>×</button>
                </div>
            )}

            <div className="chat-list">
                {users.length > 0 ? (
                    users.map(user => (
                        <div key={user.id} className="chat-item">
                            <Link to={`/chat/${user.id}`}>
                                <div className="avatar-container">
                                    <img
                                        src={`https://i.pravatar.cc/150?u=${user.id}`}
                                        alt={user.username}
                                        className="avatar"
                                    />
                                </div>
                                <div className="chat-content">
                                    <h3>{user.username}</h3>
                                    <span className="user-id">ID: {user.id}</span>
                                </div>
                            </Link>
                        </div>
                    ))
                ) : (
                    <div className="no-results">
                        <p>
                            {searchQuery.trim()
                                ? 'Пользователи не найдены'
                                : 'Введите имя для поиска пользователей'
                            }
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
}

export default ChatsPage;
