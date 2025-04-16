import React, {useState, useEffect} from 'react';
import {useParams, useNavigate} from 'react-router-dom';
import './PersChatPage.css';
import DiffieHellman from './DH/DiffieHellman';
import {P, G} from './DH/constants';

function PersChatPage() {
    const {chatId} = useParams();
    const [messages, setMessages] = useState([]);
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [dhInstance, setDhInstance] = useState(null);
    const [sharedSecret, setSharedSecret] = useState(null);
    const [, setOtherPublicKey] = useState(null);
    const [currentUserId, setCurrentUserId] = useState(null);
    const navigate = useNavigate();



    useEffect(() => {
        const fetchCurrentUser = async () => {
            const response = await fetch('http://localhost:5078/api/auth/me', {
                credentials: 'include'
            });
            const user = await response.json();
            setCurrentUserId(user.id);
        };
        fetchCurrentUser();
    }, []);

    // 2. Инициализация DH и загрузка сообщений
    useEffect(() => {
        if (!currentUserId) return;

        const initDH = async () => {
            try {
                // Генерируем ключи для текущего пользователя
                const dh = new DiffieHellman(P, G);
                setDhInstance(dh);

                // Отправляем свой публичный ключ на сервер
                await fetch(`http://localhost:5078/api/chat/${chatId}/updateKey`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    credentials: 'include',
                    body: JSON.stringify({
                        publicKey: dh.publicKey.toString()
                    })
                });

                // Пытаемся получить ключ собеседника
                const keyResponse = await fetch(`http://localhost:5078/api/chat/${chatId}/participantKey`, {
                    credentials: 'include'
                });

                if (keyResponse.ok) {
                    const {publicKey} = await keyResponse.json();
                    if (publicKey) {
                        setOtherPublicKey(publicKey);
                        const secret = dh.computeSharedSecret(publicKey);
                        setSharedSecret(secret);
                    }
                }

                // Загружаем историю сообщений
                const messagesResponse = await fetch(`http://localhost:5078/api/chat/${chatId}/history?count=50`, {
                    credentials: 'include'
                });
                setMessages(await messagesResponse.json());

            } catch (error) {
                console.error('Initialization error:', error);
            } finally {
                setIsLoading(false);
            }
        };

        initDH();
    }, [chatId, currentUserId]);

    const handleSendMessage = async () => {
        if (!newMessage.trim() || !sharedSecret) return;

        try {
            const encryptedMessage = Array.from(newMessage)
                .map(char =>
                    String.fromCharCode(char.charCodeAt(0) ^ Number(sharedSecret % 255n)))
                        .join('');

            await fetch(`http://localhost:5078/api/chat/${chatId}/send`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({message: encryptedMessage})
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

    const decryptMessage = (encrypted) => {
        if (!sharedSecret || !encrypted) return encrypted;
        return Array.from(encrypted)
            .map(char =>
                String.fromCharCode(char.charCodeAt(0) ^ Number(sharedSecret % 255n)))
                    .join('');
    };

    if (isLoading) return <div className="loading">Loading chat...</div>;

    return (
        <div className="pers-chat-container">
            <div className="chat-header">
                <button onClick={() => navigate(-1)} className="back-button">← Back</button>
                <h2>Chat #{chatId}</h2>
                {dhInstance && (
                    <div className="key-info">
                        <small>My Public Key: {dhInstance.publicKey.toString().slice(0, 10)}...</small>
                    </div>
                )}
            </div>

            <div className="messages-container">
                {messages.map((message, index) => {
                    const isCurrentUser = message.isCurrentUser;
                    const decryptedContent = decryptMessage(message.encryptedContent);

                    return (
                        <div key={index} className={`message-wrapper ${isCurrentUser ? 'sent' : 'received'}`}>
                            <div className={`message ${isCurrentUser ? 'sent' : 'received'}`}>
                                <div className="message-header">
                                    <span className="message-username">
                                        {isCurrentUser ? 'You' : message.senderUsername}
                                    </span>
                                </div>
                                <div className="message-content">{decryptedContent}</div>
                                <div className="message-time">
                                    {new Date(message.sentAt).toLocaleTimeString([], {
                                        hour: '2-digit',
                                        minute: '2-digit',
                                    })}
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            <div className="message-input">
                <input
                    type="text"
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    placeholder="Type a message..."
                    onKeyDown={(e) => e.key === 'Enter' && handleSendMessage()}
                    disabled={!sharedSecret}
                />
                <button
                    onClick={handleSendMessage}
                    disabled={!sharedSecret || !newMessage.trim()}
                >
                    Send
                </button>
                {!sharedSecret && (
                    <div className="warning">Establishing secure connection...</div>
                )}
            </div>
        </div>
    );
}

export default PersChatPage;