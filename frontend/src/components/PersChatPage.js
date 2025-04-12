import React, {useState, useEffect} from 'react';
import {useParams, useNavigate} from 'react-router-dom';
import './PersChatPage.css';

function PersChatPage() {
    const {chatId} = useParams();
    const [messages, setMessages] = useState([]);
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        const loadMessages = async () => {
            setIsLoading(true);
            try {
                const response = await fetch(`http://localhost:5078/api/chat/${chatId}/history?count=50`, {
                    credentials: 'include'
                });
                const data = await response.json();
                setMessages(data);
            } catch (error) {
                console.error('Error fetching chat history:', error);
            } finally {
                setIsLoading(false);
            }
        };

        loadMessages();
    }, [chatId]);

    const handleSendMessage = async () => {
        if (!newMessage.trim()) return;

        try {
            await fetch(`http://localhost:5078/api/chat/${chatId}/send`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({message: newMessage})
            });

            setNewMessage('');
            const response = await fetch(`http://localhost:5078/api/chat/${chatId}/history?count=50`, {
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
                <button onClick={() => navigate(-1)} className="back-button">← Back</button>
                <h2>Chat #{chatId}</h2>
            </div>

            <div className="messages-container">
                {messages.map((message, index) => (
                    <div key={index}
                         className={`message ${message.senderId === message.currentUserId ? 'sent' : 'received'}`}>
                        <div className="message-content">{message.encryptedContent}</div>
                        <div className="message-time">
                            {new Date(message.sentAt).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'})}
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
                    onKeyDown={(e) => e.key === 'Enter' && handleSendMessage()}
                />
                <button onClick={handleSendMessage}>Send</button>
            </div>
        </div>
    );
}

export default PersChatPage;
