import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import './PersChatPage.css';

function PersChatPage() {
    const { chatId } = useParams();
    const [messages, setMessages] = useState([]);
    const [newMessage, setNewMessage] = useState('');
    const [chatInfo, setChatInfo] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        const loadChatData = async () => {
            try {
                setIsLoading(true);

                // Загрузка информации о чате
                const chatResponse = await fetch(`http://localhost:5078/api/chat/${chatId}`, {
                    credentials: 'include'
                });
                const chatData = await chatResponse.json();
                setChatInfo(chatData);

                // Загрузка сообщений
                const messagesResponse = await fetch(`http://localhost:5078/api/chat/${chatId}/messages`, {
                    credentials: 'include'
                });
                const messagesData = await messagesResponse.json();
                setMessages(messagesData);
            } finally {
                setIsLoading(false);
            }
        };

        loadChatData();
    }, [chatId]);

    const handleSendMessage = async () => {
        if (!newMessage.trim()) return;

        try {
            await fetch(`http://localhost:5078/api/chat/${chatId}/messages`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ encryptedContent: newMessage })
            });

            setNewMessage('');
            // Обновляем сообщения
            const response = await fetch(`http://localhost:5078/api/chat/${chatId}/messages`, {
                credentials: 'include'
            });
            setMessages(await response.json());
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };

    if (isLoading) return <div className="loading">Loading chat...</div>;

    return (
        <div className="pers-chat-container">
            <div className="chat-header">
                <button onClick={() => navigate(-1)} className="back-button">
                    ← Back
                </button>
                {chatInfo && (
                    <div className="user-info">
                        <img src={chatInfo.Avatar} alt={chatInfo.Username} className="avatar" />
                        <h2>{chatInfo.Username}</h2>
                    </div>
                )}
            </div>

            <div className="messages-container">
                {messages.map((message, index) => (
                    <div key={index} className={`message ${message.senderId === chatInfo?.UserId ? 'received' : 'sent'}`}>
                        <div className="message-content">{message.encryptedContent}</div>
                        <div className="message-time">
                            {new Date(message.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </div>
                    </div>
                ))}
            </div>

            <div className="message-input">
                <input
                    type="text"
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    placeholder="Type a message..."
                    onKeyPress={(e) => e.key === 'Enter' && handleSendMessage()}
                />
                <button onClick={handleSendMessage}>Send</button>
            </div>
        </div>
    );
}

export default PersChatPage;