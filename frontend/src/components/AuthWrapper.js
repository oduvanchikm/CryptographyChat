import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function AuthWrapper({ children }) {
    const navigate = useNavigate();
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const checkAuth = async () => {
            try {
                const session = JSON.parse(sessionStorage.getItem('currentSession'));
                if (!session?.sessionId) {
                    throw new Error('No active session');
                }

                const response = await fetch('http://localhost:5079/api/auth/me', {
                    credentials: 'include'
                });

                if (!response.ok) {
                    throw new Error('Session expired');
                }

                setIsAuthenticated(true);
            } catch (error) {
                sessionStorage.removeItem('currentSession');
                navigate('/login', { replace: true });
            } finally {
                setIsLoading(false);
            }
        };

        checkAuth();
    }, [navigate]);

    if (isLoading) {
        return <div className="loading-spinner">Loading...</div>;
    }

    return isAuthenticated ? children : null;
}