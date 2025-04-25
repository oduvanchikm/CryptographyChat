import React, {useState, useEffect} from 'react';
import {useParams, useNavigate} from 'react-router-dom';
import './PersChatPage.css';
import DiffieHellman from './DH/DiffieHellman';
import {P, G} from './DH/constants';
/* global BigInt */

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
            const response = await fetch('http://localhost:5079/api/auth/me', {
                credentials: 'include'
            });
            const user = await response.json();
            setCurrentUserId(user.id);
        };
        fetchCurrentUser();
    }, []);

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

                await fetch(`http://localhost:5079/api/chat/${chatId}/updateKey`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    credentials: 'include',
                    body: JSON.stringify({
                        publicKey: publicKeyBase64
                    })
                });

                const keyResponse = await fetch(`http://localhost:5079/api/chat/${chatId}/participantKey`, {
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

                const messagesResponse = await fetch(`http://localhost:5079/api/chat/${chatId}/history?count=50`, {
                    credentials: 'include'
                });
                if (messagesResponse.ok) {
                    setMessages(await messagesResponse.json());
                }

            } catch (error) {
                console.error('Initialization error:', error);
                setTimeout(initDH, 5000);
            } finally {
                setIsLoading(false);
            }
        };

        initDH();
    }, [chatId, currentUserId]);

    const handleSendMessage = async () => {
        if (!newMessage.trim() || !sharedSecret) return;

        try {
            const publicKeyBase64 = bigIntToBase64(dhInstance.publicKey);
            await fetch(`http://localhost:5079/api/chat/${chatId}/send`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({
                    message: newMessage,
                    publicKey: publicKeyBase64
                })
            });

            setNewMessage('');
            const response = await fetch(`http://localhost:5079/api/chat/${chatId}/history?count=50`, {
                credentials: 'include'
            });
            setMessages(await response.json());
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