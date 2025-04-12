import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './ChatsPage.css';

function ChatsPage() {
    const [searchQuery, setSearchQuery] = useState('');
    const [users, setUsers] = useState([]);
    const [chats, setChats] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const [creatingChat, setCreatingChat] = useState(false);

    const navigate = useNavigate();

    useEffect(() => {
        const fetchChats = async () => {
            try {
                const response = await fetch(`http://localhost:5078/api/chats/userschats`, {
                    credentials: 'include'
                });
                if (!response.ok) {
                    throw new Error('Ошибка загрузки чатов');
                }
                const data = await response.json();
                setChats(data);
            } catch (err) {
                console.error('Ошибка при загрузке чатов:', err);
                setError(err.message);
            }
        };

        fetchChats().catch(console.error);
    }, []);

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
                    credentials: 'include',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                }
            );

            if (!response.ok) {
                throw new Error(`Ошибка поиска: ${response.status}`);
            }

            const data = await response.json();
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
            searchUsers(searchQuery).catch(console.error);
        }, 300);

        return () => clearTimeout(timer);
    }, [searchQuery]);

    const handleUserClick = async (participantId) => {
        if (creatingChat) return;
        setCreatingChat(true);
        setError(null);

        try {
            const response = await fetch(`http://localhost:5078/api/chat/create`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ participantId })
            });

            if (!response.ok) {
                throw new Error('Ошибка при создании чата');
            }

            const data = await response.json();

            navigate(`/chat/${data.chatId}`);
        } catch (error) {
            setError(error.message);
            console.error('Ошибка при создании чата:', error);
        } finally {
            setCreatingChat(false);
        }
    };

    const handleChatClick = (id) => {
        navigate(`/chat/${id}`);
    };

    return (
        <div className="chats-container">
            <div className="chats-header">
                <h1>SecureChat</h1>

                <div className="section">
                    <h2>Текущие чаты</h2>
                    {chats.length > 0 ? (
                        <div className="chat-list">
                            {chats.map(chat => (
                                <div key={chat.id} className="chat-item" onClick={() => handleChatClick(chat.id)}>
                                    <div className="avatar-container">
                                        <img src={chat.avatar} alt={chat.name} className="avatar" />
                                    </div>
                                    <div className="chat-content">
                                        <h3>{chat.name}</h3>
                                        <span className="chat-id">Чат ID: {chat.id}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <p>У вас пока нет чатов</p>
                    )}
                </div>

                <div className="section">
                    <h2>Новый чат</h2>
                    <div className="search-container">
                        <input
                            type="text"
                            placeholder="Поиск по имени пользователя"
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            disabled={isLoading || creatingChat}
                        />

                        {isLoading && (
                            <div className="search-loading">
                                <div className="spinner"></div>
                                <span>Поиск...</span>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {error && (
                <div className="error-message">
                    {error}
                    <button onClick={() => setError(null)}>×</button>
                </div>
            )}

            <div
                className="chat-list"
                style={{
                    pointerEvents: creatingChat ? 'none' : 'auto',
                    opacity: creatingChat ? 0.6 : 1
                }}
            >
                {users.length > 0 ? (
                    users.map(user => (
                        <div key={user.id} className="chat-item" onClick={() => handleUserClick(user.id)}>
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
                        </div>
                    ))
                ) : (
                    <div className="no-results">
                        <p>
                            {searchQuery.trim()
                                ? 'Пользователи не найдены'
                                : 'Введите имя для поиска пользователей'}
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
}

export default ChatsPage;
