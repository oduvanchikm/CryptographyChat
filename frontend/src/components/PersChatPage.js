import React, {useState, useEffect, useCallback} from 'react';
import {useParams, useNavigate} from 'react-router-dom';
import './PersChatPage.css';
import DiffieHellman from './DH/DiffieHellman';
import {P, G} from './DH/constants';
/* global BigInt */

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL;

function bigIntToBase64(bigint) {
    const hex = bigint.toString(16);
    const paddedHex = hex.length % 2 === 0 ? hex : '0' + hex;
    const byteArray = new Uint8Array(paddedHex.match(/.{1,2}/g).map(byte => parseInt(byte, 16)));
    const binaryString = String.fromCharCode(...byteArray);
    return btoa(binaryString);
}

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
            const response = await fetch(`${API_BASE_URL}/auth/me`, {
                credentials: 'include'
            });
            const user = await response.json();
            setCurrentUserId(user.id);
        };
        fetchCurrentUser();
    }, []);

    const loadMessages = useCallback(async (onlyNew = false) => {
        try {
            const response = await fetch(`${API_BASE_URL}/chat/${chatId}/history?count=50`, {
                credentials: 'include'
            });

            if (response.ok) {
                const newMessages = await response.json();

                setMessages(prevMessages => {
                    if (onlyNew) {
                        const existingIds = new Set(prevMessages.map(m =>
                            `${m.sentAt}-${m.senderId}-${m.encryptedContent}`
                        ));
                        const filteredNewMessages = newMessages.filter(
                            msg => !existingIds.has(`${msg.sentAt}-${msg.senderId}-${msg.encryptedContent}`)
                        );
                        return [...prevMessages, ...filteredNewMessages].sort((a, b) =>
                            new Date(a.sentAt) - new Date(b.sentAt)
                        );
                    } else {
                        return newMessages.sort((a, b) =>
                            new Date(a.sentAt) - new Date(b.sentAt)
                        );
                    }
                });
            }
        } catch (error) {
            console.error('Failed to load messages:', error);
        }
    }, [chatId]);

    useEffect(() => {
        if (!currentUserId) return;

        function base64ToBigInt(base64) {
            try {
                const binaryStr = atob(base64);
                const hex = Array.from(binaryStr)
                    .map(c => c.charCodeAt(0).toString(16).padStart(2, '0'))
                    .join('');
                return BigInt('0x' + hex);
            } catch (e) {
                console.error('Error converting base64 to BigInt:', e);
                throw new Error('Invalid public key format');
            }
        }

        const initDH = async () => {
            try {
                const dh = new DiffieHellman(P, G);
                setDhInstance(dh);

                const publicKeyBase64 = bigIntToBase64(dh.publicKey);

                await fetch(`${API_BASE_URL}/chat/${chatId}/updateKey`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    credentials: 'include',
                    body: JSON.stringify({
                        publicKey: publicKeyBase64
                    })
                });

                const keyResponse = await fetch(`${API_BASE_URL}/chat/${chatId}/participantKey`, {
                    credentials: 'include'
                });

                if (keyResponse.ok) {
                    const {publicKey} = await keyResponse.json();
                    if (publicKey) {
                        try {
                            const otherPubKey = base64ToBigInt(publicKey);
                            const secret = dh.computeSharedSecret(otherPubKey);
                            setSharedSecret(secret);
                            setOtherPublicKey(publicKey);
                        } catch (e) {
                            console.error('Error computing shared secret:', e);
                            setTimeout(initDH, 5000);
                            return;
                        }
                    }
                }

                await loadMessages(false);

            } catch (error) {
                console.error('Initialization error:', error);
                setTimeout(initDH, 5000);
            } finally {
                setIsLoading(false);
            }
        };

        initDH();
    }, [chatId, currentUserId, loadMessages]);

    useEffect(() => {
        if (!chatId || !currentUserId) return;

        const intervalId = setInterval(() => {
            loadMessages(true);
        }, 3000);

        return () => clearInterval(intervalId);
    }, [chatId, currentUserId, loadMessages]);

    const handleSendMessage = async () => {
        if (!newMessage.trim() || !sharedSecret) return;

        try {
            const publicKeyBase64 = bigIntToBase64(dhInstance.publicKey);
            await fetch(`${API_BASE_URL}/chat/${chatId}/send`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({
                    message: newMessage,
                    publicKey: publicKeyBase64
                })
            });

            setNewMessage('');
            await loadMessages(true);
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };

    const decryptMessage = (encrypted) => encrypted;

    if (isLoading) return <div className="loading">Loading chat...</div>;

    return (
        <div className="pers-chat-container">
            <div className="chat-header">
                <button onClick={() => navigate(-1)} className="back-button">← Back</button>
                <h2>Chat #{chatId}</h2>
            </div>

            <div className="messages-container">
                {messages.map((message, index) => {
                    const isCurrentUser = message.isCurrentUser;
                    const decryptedContent = decryptMessage(message.encryptedContent);

                    return (
                        <div key={`${message.sentAt}-${message.senderId}-${index}`}
                             className={`message-wrapper ${isCurrentUser ? 'sent' : 'received'}`}>
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