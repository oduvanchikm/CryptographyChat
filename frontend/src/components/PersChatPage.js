import React, {useState, useEffect, useCallback} from 'react';
import {useParams, useNavigate} from 'react-router-dom';
import './PersChatPage.css';
import DiffieHellman from './DH/DiffieHellman';
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
    const [isUploading, setIsUploading] = useState(false);
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
                const dh = new DiffieHellman(2048);
                setDhInstance(dh);

                const publicKeyBase64 = bigIntToBase64(dh.publicKey);

                await fetch(`${API_BASE_URL}/chat/${chatId}/updateKey`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    credentials: 'include',
                    body: JSON.stringify({publicKey: publicKeyBase64})
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

        const intervalId = setInterval(async () => {
            await loadMessages(true);

            if (dhInstance && !sharedSecret) {
                try {
                    const keyResponse = await fetch(`${API_BASE_URL}/chat/${chatId}/participantKey`, {
                        credentials: 'include'
                    });
                    if (keyResponse.ok) {
                        const {publicKey} = await keyResponse.json();
                        if (publicKey) {
                            const otherPubKey = base64ToBigInt(publicKey);
                            const secret = dhInstance.computeSharedSecret(otherPubKey);
                            setSharedSecret(secret);
                            setOtherPublicKey(publicKey);
                            console.log('🔐 Shared secret established via polling');
                        }
                    }
                } catch (e) {
                    console.error('Polling: Failed to update DH shared secret:', e);
                }
            }
        }, 3000);

        return () => clearInterval(intervalId);
    }, [chatId, currentUserId, dhInstance, sharedSecret, loadMessages]);

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
                    publicKey: publicKeyBase64,
                    contentType: 'text'
                })
            });

            setNewMessage('');
            await loadMessages(true);
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };

    const handleFileUpload = async (e) => {
        const file = e.target.files[0];
        if (!file || !sharedSecret) return;

        if (file.size === 0) {
            alert('Cannot send empty file');
            e.target.value = '';
            return;
        }

        const maxSize = file.type.startsWith('video/') ? 10 * 1024 * 1024 : 5 * 1024 * 1024;
        if (file.size > maxSize) {
            alert(`File size exceeds ${maxSize / 1024 / 1024}MB limit`);
            return;
        }

        setIsUploading(true);

        try {
            const arrayBuffer = await file.arrayBuffer();
            const base64Content = btoa(
                new Uint8Array(arrayBuffer).reduce((data, byte) => data + String.fromCharCode(byte), '')
            );

            const publicKeyBase64 = bigIntToBase64(dhInstance.publicKey);

            await fetch(`${API_BASE_URL}/chat/${chatId}/send`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({
                    message: base64Content,
                    publicKey: publicKeyBase64,
                    contentType: file.type || 'application/octet-stream',
                    fileName: file.name
                })
            });

            await loadMessages(true);
        } catch (error) {
            console.error('File upload failed:', error);
        } finally {
            setIsUploading(false);
            e.target.value = '';
        }
    };

    const downloadFile = (base64Content, fileName, contentType, isImage = false) => {
        try {
            console.log("Base64 content:", base64Content.encryptedContent);
            const binaryString = atob(base64Content);
            const byteArray = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                byteArray[i] = binaryString.charCodeAt(i);
            }

            const blob = new Blob([byteArray], {type: contentType});
            const url = window.URL.createObjectURL(blob);

            if (isImage) {
                window.open(url, '_blank');
            } else {
                const a = document.createElement('a');
                a.href = url;
                a.download = fileName || 'download';
                document.body.appendChild(a);
                a.click();

                setTimeout(() => {
                    document.body.removeChild(a);
                    window.URL.revokeObjectURL(url);
                }, 100);
            }
        } catch (error) {
            console.error('Failed to handle file:', error);
            alert('Failed to download file. See console for details.');
        }
    };

    const decryptMessage = (encrypted) => encrypted;

    if (isLoading) return <div className="loading">Loading chat...</div>;

    const FileIcon = ({fileType, fileName}) => {
        const getFileIcon = () => {
            const extension = fileName?.split('.').pop().toLowerCase();

            if (fileType === 'application/pdf' || extension === 'pdf') {
                return (
                    <div className="file-icon pdf">
                        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <path
                                d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z"
                                fill="#FF5252"/>
                            <path d="M14 2V8H20" fill="#FF7B7B"/>
                            <path d="M16 13H8V11H16V13Z" fill="white"/>
                            <path d="M16 17H8V15H16V17Z" fill="white"/>
                            <path d="M10 9H9V10H10V9Z" fill="white"/>
                        </svg>
                    </div>
                );
            }

            if (fileType === 'text/plain' || extension === 'txt') {
                return (
                    <div className="file-icon txt">
                        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <path
                                d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z"
                                fill="#2196F3"/>
                            <path d="M14 2V8H20" fill="#64B5F6"/>
                            <path d="M16 13H8V11H16V13Z" fill="white"/>
                            <path d="M16 17H8V15H16V17Z" fill="white"/>
                            <path d="M16 9H8V7H16V9Z" fill="white"/>
                        </svg>
                    </div>
                );
            }

            return (
                <div className="file-icon generic">
                    <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path
                            d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z"
                            fill="#9E9E9E"/>
                        <path d="M14 2V8H20" fill="#BDBDBD"/>
                        <path d="M16 13H8V11H16V13Z" fill="white"/>
                        <path d="M16 17H8V15H16V17Z" fill="white"/>
                    </svg>
                </div>
            );
        };

        return getFileIcon();
    };

    return (
        <div className="pers-chat-container">
            <div className="chat-header">
                <button onClick={() => navigate(-1)} className="back-button">← Back</button>
                <h2>Chat #{chatId}</h2>
            </div>

            <div className="messages-container">
                {messages.map((message, index) => {
                    const isCurrentUser = message.isCurrentUser;

                    const isMedia = message.contentType && (
                        message.contentType.startsWith('image/') ||
                        message.contentType.startsWith('video/') ||
                        message.contentType.startsWith('audio/')
                    );

                    const isPdf = message.contentType === 'application/pdf';

                    if (isMedia) {
                        return (
                            <div key={`${message.sentAt}-${message.senderId}-${index}`}
                                 className={`message-wrapper ${isCurrentUser ? 'sent' : 'received'}`}>
                                <div className={`message ${isCurrentUser ? 'sent' : 'received'}`}>
                                    <div className="message-header">
          <span className="message-username">
            {isCurrentUser ? 'You' : message.senderUsername}
          </span>
                                    </div>
                                    <div className="message-content">
                                        {message.contentType.startsWith('image/') ? (
                                            <img
                                                src={`data:${message.contentType};base64,${message.encryptedContent}`}
                                                alt={message.fileName || "Sent image"}
                                                className="chat-image"
                                                onClick={() => downloadFile(
                                                    message.encryptedContent,
                                                    message.fileName,
                                                    message.contentType,
                                                    true
                                                )}
                                            />
                                        ) : message.contentType.startsWith('video/') ? (
                                            <video controls className="chat-video">
                                                <source
                                                    src={`data:${message.contentType};base64,${message.encryptedContent}`}
                                                    type={message.contentType}
                                                />
                                                Your browser does not support the video tag.
                                            </video>
                                        ) : message.contentType.startsWith('audio/') ? (
                                            <audio controls className="chat-audio">
                                                <source
                                                    src={`data:${message.contentType};base64,${message.encryptedContent}`}
                                                    type={message.contentType}
                                                />
                                                Your browser does not support the audio element.
                                            </audio>
                                        ) : null}
                                    </div>
                                    <div className="message-time">
                                        {new Date(message.sentAt).toLocaleTimeString([], {
                                            hour: '2-digit',
                                            minute: '2-digit',
                                        })}
                                    </div>
                                </div>
                            </div>
                        );
                    } else if (isPdf || (message.contentType && message.contentType !== 'text')) {
                        return (
                            <div key={`${message.sentAt}-${message.senderId}-${index}`}
                                 className={`message-wrapper ${isCurrentUser ? 'sent' : 'received'}`}>
                                <div className={`message ${isCurrentUser ? 'sent' : 'received'}`}>
                                    <div className="message-header">
                        <span className="message-username">
                            {isCurrentUser ? 'You' : message.senderUsername}
                        </span>
                                    </div>
                                    <div className="message-content file-message">
                                        <div className="file-header">
                                            <FileIcon fileType={message.contentType} fileName={message.fileName}/>
                                            <div className="file-meta">
                                                <div className="file-name">{message.fileName}</div>
                                                <div className="file-size">
                                                    {Math.round(message.encryptedContent.length / 1024)} KB
                                                </div>
                                            </div>
                                        </div>
                                        <button
                                            className="download-button"
                                            onClick={() => downloadFile(
                                                message.encryptedContent,
                                                message.fileName,
                                                message.contentType
                                            )}
                                        >
                                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                                                 xmlns="http://www.w3.org/2000/svg" style={{marginRight: '8px'}}>
                                                <path d="M12 15V3M12 15L8 11M12 15L16 11" stroke="currentColor"
                                                      strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                                                <path
                                                    d="M20 17V19C20 20.1046 19.1046 21 18 21H6C4.89543 21 4 20.1046 4 19V17"
                                                    stroke="currentColor" strokeWidth="2" strokeLinecap="round"
                                                    strokeLinejoin="round"/>
                                            </svg>
                                            Download
                                        </button>
                                    </div>
                                    <div className="message-time">
                                        {new Date(message.sentAt).toLocaleTimeString([], {
                                            hour: '2-digit',
                                            minute: '2-digit',
                                        })}
                                    </div>
                                </div>
                            </div>
                        );
                    }

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
                <input
                    type="file"
                    id="file-upload"
                    onChange={handleFileUpload}
                    style={{display: 'none'}}
                    disabled={!sharedSecret}
                />
                <label htmlFor="file-upload" className="file-upload-button">
                    {isUploading ? (
                        <div className="upload-spinner"></div>
                    ) : (
                        '📎 Add File'
                    )}
                </label>
                {!sharedSecret && (
                    <div className="warning">Establishing secure connection...</div>
                )}
            </div>
        </div>
    );
}

export default PersChatPage;