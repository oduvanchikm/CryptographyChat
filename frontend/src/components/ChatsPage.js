import React, {useEffect, useState} from 'react';
import {Link} from 'react-router-dom';
import './ChatsPage.css';

function ChatsPage() {
    const [searchQuery, setSearchQuery] = useState('');
    const [activeTab, setActiveTab] = useState('all');
    const [searchResults, setSearchResults] = useState([]);
    const [chats, setChats] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        const loadChats = async () => {
            try {
                setIsLoading(true);
                const response = await fetch(`http://localhost:5078/api/chats/userschats?search=${encodeURIComponent(searchQuery)}`, {
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    }
                });

                if (!response.ok) throw new Error('Failed to load chats');
                const data = await response.json();
                setChats(data);
            } catch (err) {
                setError(err.message);
            } finally {
                setIsLoading(false);
            }
        };

        const debounceTimer = setTimeout(loadChats, 300);
        return () => clearTimeout(debounceTimer);
    }, [searchQuery]);

    useEffect(() => {
        const searchUsers = async () => {
            if (searchQuery.trim() === '') {
                setSearchResults([]);
                return;
            }

            setIsLoading(true);
            setError(null);

            try {
                const response = await fetch(`http://localhost:5078/api/chats/users?search=${encodeURIComponent(searchQuery)}`, {
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    }
                });

                if (!response.ok) throw new Error('Search failed');

                const data = await response.json();
                const users = data.map(user => ({
                    id: user.id,
                    name: user.username,
                    lastMessage: user.lastLogin ? `Last seen: ${new Date(user.lastLogin).toLocaleString()}` : 'Never logged in',
                    time: '',
                    unread: 0,
                    isOnline: isUserOnline(user.lastLogin),
                    avatar: `https://i.pravatar.cc/150?u=${user.id}`,
                }));

                setSearchResults(users);
            } catch (err) {
                setError(err.message);
            } finally {
                setIsLoading(false);
            }
        };

        const debounceTimer = setTimeout(searchUsers, 500);
        return () => clearTimeout(debounceTimer);
    }, [searchQuery]);

    const isUserOnline = (lastLogin) => {
        if (!lastLogin) return false;
        const lastSeen = new Date(lastLogin);
        return (Date.now() - lastSeen.getTime()) < (15 * 60 * 1000);
    };

    const formatTime = (dateString) => {
        const date = new Date(dateString);
        const now = new Date();

        if (date.toDateString() === now.toDateString()) {
            return date.toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'});
        }

        if (date.getFullYear() === now.getFullYear()) {
            return date.toLocaleDateString([], {month: 'short', day: 'numeric'});
        }

        return date.toLocaleDateString([], {year: 'numeric', month: 'short', day: 'numeric'});
    };

    const displayedChats = searchQuery.trim() !== ''
        ? [
            ...chats.filter(chat =>
                chat.name.toLowerCase().includes(searchQuery.toLowerCase())),
            ...searchResults.filter(user =>
                !chats.some(chat => chat.id === user.id))
        ]
        : chats;

    return (
        <div className="chats-container">
            <div className="chats-header">
                <h1>SecureChat</h1>
                <div className="search-container">
                    <input
                        type="text"
                        placeholder="Search people or groups"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                    />
                    <svg className="search-icon" viewBox="0 0 24 24">
                        <path
                            d="M15.5 14h-.79l-.28-.27a6.5 6.5 0 0 0 1.48-5.34c-.47-2.78-2.79-5-5.59-5.34a6.505 6.505 0 0 0-7.27 7.27c.34 2.8 2.56 5.12 5.34 5.59a6.5 6.5 0 0 0 5.34-1.48l.27.28v.79l4.25 4.25c.41.41 1.08.41 1.49 0 .41-.41.41-1.08 0-1.49L15.5 14zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
                    </svg>
                    {isLoading && <div className="search-loading">Searching...</div>}
                    {error && <div className="search-error">{error}</div>}
                </div>
            </div>

            <div className="chat-tabs">
                <button
                    className={activeTab === 'all' ? 'active' : ''}
                    onClick={() => setActiveTab('all')}
                >
                    All Chats
                </button>
            </div>

            <div className="chat-list">
                {isLoading && !searchQuery ? (
                    <div className="no-chats">Loading chats...</div>
                ) : displayedChats.length > 0 ? (
                    displayedChats.map(chat => (
                        <Link to={`/chat/${chat.id}`} key={chat.id} className="chat-item">
                            <div className="avatar-container">
                                <img src={chat.avatar} alt={chat.name} className="avatar"/>
                                {chat.isOnline && !chat.isGroup && <span className="online-badge"></span>}
                            </div>
                            <div className="chat-content">
                                <div className="chat-header">
                                    <h3>{chat.name}</h3>
                                    <span className="time">{formatTime(chat.time)}</span>
                                </div>
                                <p className="last-message">{chat.lastMessage}</p>
                            </div>
                            {chat.unread > 0 && (
                                <span className="unread-count">{chat.unread}</span>
                            )}
                        </Link>
                    ))
                ) : (
                    <div className="no-chats">
                        <p>{searchQuery.trim() ? 'No results found' : 'No chats available'}</p>
                    </div>
                )}
            </div>

            <button className="nav-button active">
                <svg viewBox="0 0 24 24">
                    <path
                        d="M19.25 3.018H4.75C3.233 3.018 2 4.252 2 5.77v12.495c0 1.518 1.233 2.753 2.75 2.753h14.5c1.517 0 2.75-1.235 2.75-2.753V5.77c0-1.518-1.233-2.752-2.75-2.752zm-14.5 1.5h14.5c.69 0 1.25.56 1.25 1.25v.714l-8.05 5.367c-.273.18-.626.182-.9-.002L3.5 6.482v-.714c0-.69.56-1.25 1.25-1.25zm14.5 14.998H4.75c-.69 0-1.25-.56-1.25-1.25V8.24l7.24 4.83c.383.256.822.384 1.26.384.44 0 .877-.128 1.26-.383l7.24-4.83v10.022c0 .69-.56 1.25-1.25 1.25z"/>
                </svg>
                <span>Chats</span>
            </button>
            <button className="nav-button">
                <svg viewBox="0 0 24 24">
                    <path
                        d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"/>
                </svg>
                <span>Contacts</span>
            </button>
            <button className="nav-button">
                <svg viewBox="0 0 24 24">
                    <path
                        d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"/>
                </svg>
                <span>Settings</span>
            </button>
        </div>
    );
}

export default ChatsPage;