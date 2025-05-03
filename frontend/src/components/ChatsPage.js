import React, {useEffect, useState, useRef, useCallback} from 'react';
import {useNavigate} from 'react-router-dom';
import './ChatsPage.css';

function ChatsPage() {
    const [searchQuery, setSearchQuery] = useState('');
    const [users, setUsers] = useState([]);
    const [chats, setChats] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const [creatingChat, setCreatingChat] = useState(false);
    const [isSearchFocused, setIsSearchFocused] = useState(false);
    const chatsContainerRef = useRef(null);

    const [currentUser, setCurrentUser] = useState(null);

    const CRYPTO_ALGORITHMS = ['RC5', 'MARS'];
    const PADDING_OPTIONS = ['PKCS7', 'Zeros', 'ANSIX923', 'ISO10126'];
    const MODE_OPTIONS = ['CBC', 'CFB', 'ECB', 'OFB', 'CTR', 'PCBC', 'RD'];

    const [cryptoConfig, setCryptoConfig] = useState({
        algorithm: 'RC5',
        padding: 'PKCS7',
        mode: 'CBC'
    });

    const fetchCurrentUser = useCallback(async () => {
        try {
            const response = await fetch(`http://localhost:5079/api/auth/me`, {
                credentials: 'include'
            });
            if (!response.ok) {
                throw new Error('Error');
            }
            const data = await response.json();
            setCurrentUser(data);
        } catch (err) {
            console.error('Error:', err);
        }
    }, []);

    const navigate = useNavigate();

    const fetchChats = useCallback(async () => {
        try {
            const response = await fetch(`http://localhost:5079/api/chats/userschats`, {
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
    }, []);

    const deleteChat = useCallback(async (chatId, event) => {
        event.stopPropagation();

        if (!window.confirm('Are you sure you want to delete this chat?')) {
            return;
        }

        try {
            const response = await fetch(`http://localhost:5079/api/chat/${chatId}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error('Failed to delete chat');
            }

            await fetchChats();
        } catch (error) {
            setError(error.message);
            console.error('Error deleting chat:', error);
        }
    }, [fetchChats]);

    useEffect(() => {
        fetchCurrentUser();
        fetchChats().catch(console.error);
    }, [fetchChats, fetchCurrentUser]);

    const searchUsers = useCallback(async (query) => {
        if (!query.trim()) {
            setUsers([]);
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const response = await fetch(
                `http://localhost:5079/api/chats/users?search=${encodeURIComponent(query)}`,
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
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => {
            searchUsers(searchQuery).catch(console.error);
        }, 300);

        return () => clearTimeout(timer);
    }, [searchQuery, searchUsers]);

    const handleUserClick = useCallback(async (participantId) => {
        if (creatingChat) return;
        setCreatingChat(true);
        setError(null);

        try {
            const requestBody = JSON.stringify({
                participantId,
                algorithm: cryptoConfig.algorithm,
                padding: cryptoConfig.padding,
                modeCipher: cryptoConfig.mode
            });

            const response = await fetch(`http://localhost:5079/api/chat/create`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: requestBody
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Ошибка при создании чата');
            }

            const data = await response.json();

            navigate(`/chat/${data.chatId}`, {
                state: {
                    chatId: data.chatId,
                    cryptoConfig
                }
            });
        } catch (error) {
            setError(error.message);
            console.error('Ошибка при создании чата:', error);
        } finally {
            setCreatingChat(false);
        }
    }, [creatingChat, cryptoConfig, navigate]);

    const handleChatClick = useCallback((id) => {
        navigate(`/chat/${id}`);
    }, [navigate]);

    const renderCryptoSettings = () => (
        <div className="crypto-settings">
            <div className="crypto-option">
                <label>Algorithm:</label>
                <select
                    value={cryptoConfig.algorithm}
                    onChange={(e) => setCryptoConfig(prev => ({...prev, algorithm: e.target.value}))}
                    disabled={creatingChat}
                >
                    {CRYPTO_ALGORITHMS.map(alg => (
                        <option key={alg} value={alg}>{alg}</option>
                    ))}
                </select>
            </div>

            <div className="crypto-option">
                <label>Padding:</label>
                <select
                    value={cryptoConfig.padding}
                    onChange={(e) => setCryptoConfig(prev => ({...prev, padding: e.target.value}))}
                    disabled={creatingChat}
                >
                    {PADDING_OPTIONS.map(pad => (
                        <option key={pad} value={pad}>{pad}</option>
                    ))}
                </select>
            </div>

            <div className="crypto-option">
                <label>Mode:</label>
                <select
                    value={cryptoConfig.mode}
                    onChange={(e) => setCryptoConfig(prev => ({...prev, mode: e.target.value}))}
                    disabled={creatingChat}
                >
                    {MODE_OPTIONS.map(mode => (
                        <option key={mode} value={mode}>{mode}</option>
                    ))}
                </select>
            </div>
        </div>
    );

    return (
        <div className="app-container">
            <div className="app-header">
                <div className="header-content">
                    <h1 className="app-title">SecureChat</h1>
                    <div className="header-right-section">
                        {currentUser && (
                            <div className="user-profile">
                                <div className="user-info">
                                    <span className="username">{currentUser.username}</span>
                                    <div className="avatar-container">
                                        <img
                                            src={`https://i.pravatar.cc/150?u=${currentUser.id}`}
                                            alt={currentUser.username}
                                            className="user-avatar"
                                            onError={(e) => {
                                                e.target.src = 'https://via.placeholder.com/150';
                                            }}
                                        />
                                    </div>
                                </div>
                            </div>
                        )}

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
            </div>

            <div className="app-content" ref={chatsContainerRef}>
                {isSearchFocused || searchQuery.trim() ? (
                    <div className="search-section">
                        <h2 className="section-title">Search Results</h2>

                        {renderCryptoSettings()}

                        {users.length > 0 ? (
                            <ul className="users-grid">
                                {users.map(user => (
                                    <li
                                        key={user.id}
                                        className="user-card"
                                        onClick={() => handleUserClick(user.id)}
                                    >
                                        <div className="user-avatar">
                                            <img
                                                src={`https://i.pravatar.cc/150?u=${user.id}`}
                                                alt={user.username}
                                                onError={(e) => {
                                                    e.target.src = 'https://via.placeholder.com/150';
                                                }}
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
                                            <img
                                                src={chat.avatar || 'https://via.placeholder.com/150'}
                                                alt={chat.name}
                                                onError={(e) => {
                                                    e.target.src = 'https://via.placeholder.com/150';
                                                }}
                                            />
                                        </div>
                                        <div className="chat-info">
                                            <div className="chat-header">
                                                <h3 className="chat-name">{chat.name}</h3>
                                                <span className="chat-time">
                                                    {chat.lastMessageTime ?
                                                        new Date(chat.lastMessageTime).toLocaleTimeString([], {
                                                            hour: '2-digit',
                                                            minute: '2-digit'
                                                        }) : ''
                                                    }
                                                </span>
                                            </div>
                                            <p className="chat-preview">
                                                {chat.lastMessage || 'No messages yet'}
                                            </p>
                                        </div>
                                        <button className="delete-chat-btn" onClick={(e) => deleteChat(chat.id, e)}
                                                title="Delete chat">
                                            🗑️
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <div className="empty-state">
                                <p>You don't have any chats yet</p>
                                <button
                                    className="new-chat-btn"
                                    onClick={() => setIsSearchFocused(true)}
                                    disabled={creatingChat}
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
                        aria-label="Close error"
                    >
                        &times;
                    </button>
                </div>
            )}
        </div>
    );
}

export default ChatsPage;