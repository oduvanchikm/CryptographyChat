import React, { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './ChatsPage.css';
import DiffieHellman from './DH/DiffieHellman';
import { P, G } from './DH/constants';

function ChatsPage() {
    const [searchQuery, setSearchQuery] = useState('');
    const [users, setUsers] = useState([]);
    const [chats, setChats] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const [creatingChat, setCreatingChat] = useState(false);
    const [isSearchFocused, setIsSearchFocused] = useState(false);
    const chatsContainerRef = useRef(null);

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
            const dh = new DiffieHellman(P, G);

            const response = await fetch(`http://localhost:5078/api/chat/create`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({
                    participantId,
                    publicKey: dh.publicKey.toString()
                })
            });

            if (!response.ok) {
                throw new Error('Ошибка при создании чата');
            }

            const data = await response.json();

            navigate(`/chat/${data.chatId}`, {
                state: {
                    publicKey: dh.publicKey.toString(),
                    chatId: data.chatId
                }
            });
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
        <div className="telegram-style-container">
            <div className="telegram-header">
                <h1>SecureChat</h1>
                <div className={`search-container ${isSearchFocused ? 'focused' : ''}`}>
                    <input
                        type="text"
                        placeholder="Поиск"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        onFocus={() => setIsSearchFocused(true)}
                        onBlur={() => setIsSearchFocused(false)}
                        disabled={isLoading || creatingChat}
                    />
                    {isLoading && (
                        <div className="search-loading">
                            <div className="spinner"></div>
                        </div>
                    )}
                </div>
            </div>

            <div className="telegram-content" ref={chatsContainerRef}>
                {isSearchFocused || searchQuery.trim() ? (
                    <div className="search-results">
                        <h2>Result</h2>
                        {users.length > 0 ? (
                            <div className="users-list">
                                {users.map(user => (
                                    <div key={user.id} className="user-item" onClick={() => handleUserClick(user.id)}>
                                        <div className="avatar-container">
                                            <img
                                                src={`https://i.pravatar.cc/150?u=${user.id}`}
                                                alt={user.username}
                                                className="avatar"
                                            />
                                        </div>
                                        <div className="user-info">
                                            <h3>{user.username}</h3>
                                            <span className="user-id">ID: {user.id}</span>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="no-results">
                                {searchQuery.trim() ? 'User not found' : 'Start finding'}
                            </div>
                        )}
                    </div>
                ) : (
                    <div className="chats-list">
                        <h2>Chats</h2>
                        {chats.length > 0 ? (
                            chats.map(chat => (
                                <div key={chat.id} className="chat-item" onClick={() => handleChatClick(chat.id)}>
                                    <div className="avatar-container">
                                        <img src={chat.avatar} alt={chat.name} className="avatar" />
                                    </div>
                                    <div className="chat-info">
                                        <div className="chat-header">
                                            <h3>{chat.name}</h3>
                                            <span className="chat-time">{new Date(chat.lastMessageTime).toLocaleTimeString()}</span>
                                        </div>
                                        <p className="chat-preview">{chat.lastMessage || 'No messages'}</p>
                                    </div>
                                </div>
                            ))
                        ) : (
                            <div className="no-chats">
                                <p>No chats</p>
                            </div>
                        )}
                    </div>
                )}
            </div>

            {error && (
                <div className="error-message">
                    {error}
                    <button onClick={() => setError(null)}>×</button>
                </div>
            )}
        </div>
    );
}

export default ChatsPage;