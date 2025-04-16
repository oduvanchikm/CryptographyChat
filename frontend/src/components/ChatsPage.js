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
        <div className="app-container">
            <div className="app-header">
                <div className="header-content">
                    <h1 className="app-title">SecureChat</h1>
                    <div className={`search-wrapper ${isSearchFocused ? 'focused' : ''}`}>
                        <input
                            type="text"
                            placeholder="Search users..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            onFocus={() => setIsSearchFocused(true)}
                            onBlur={() => setIsSearchFocused(false)}
                            disabled={isLoading || creatingChat}
                            className="search-input"
                        />
                        {isLoading && (
                            <div className="search-loader">
                                <div className="loader"></div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <div className="app-content" ref={chatsContainerRef}>
                {isSearchFocused || searchQuery.trim() ? (
                    <div className="search-section">
                        <h2 className="section-title">Search Results</h2>
                        {users.length > 0 ? (
                            <ul className="users-grid">
                                {users.map(user => (
                                    <li key={user.id} className="user-card" onClick={() => handleUserClick(user.id)}>
                                        <div className="user-avatar">
                                            <img
                                                src={`https://i.pravatar.cc/150?u=${user.id}`}
                                                alt={user.username}
                                            />
                                        </div>
                                        <div className="user-details">
                                            <h3 className="user-name">{user.username}</h3>
                                            <p className="user-meta">ID: {user.id}</p>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <div className="empty-state">
                                <p>{searchQuery.trim() ? 'No users found' : 'Start typing to search'}</p>
                            </div>
                        )}
                    </div>
                ) : (
                    <div className="chats-section">
                        <h2 className="section-title">Your Chats</h2>
                        {chats.length > 0 ? (
                            <ul className="chats-list">
                                {chats.map(chat => (
                                    <li key={chat.id} className="chat-item" onClick={() => handleChatClick(chat.id)}>
                                        <div className="chat-avatar">
                                            <img src={chat.avatar} alt={chat.name} />
                                        </div>
                                        <div className="chat-info">
                                            <div className="chat-header">
                                                <h3 className="chat-name">{chat.name}</h3>
                                                <span className="chat-time">
                                                    {new Date(chat.lastMessageTime).toLocaleTimeString([], {
                                                        hour: '2-digit',
                                                        minute: '2-digit'
                                                    })}
                                                </span>
                                            </div>
                                            <p className="chat-preview">
                                                {chat.lastMessage || 'No messages yet'}
                                            </p>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <div className="empty-state">
                                <p>You don't have any chats yet</p>
                                <button
                                    className="new-chat-btn"
                                    onClick={() => setIsSearchFocused(true)}
                                >
                                    Start New Chat
                                </button>
                            </div>
                        )}
                    </div>
                )}
            </div>

            {error && (
                <div className="error-notification">
                    <p>{error}</p>
                    <button
                        className="close-error"
                        onClick={() => setError(null)}
                    >
                        &times;
                    </button>
                </div>
            )}
        </div>
    );
}

export default ChatsPage;