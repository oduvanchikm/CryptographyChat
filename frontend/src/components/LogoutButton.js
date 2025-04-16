import { useNavigate } from 'react-router-dom';

export default function LogoutButton() {
    const navigate = useNavigate();

    const handleLogout = async () => {
        try {
            const response = await fetch('http://localhost:5078/api/auth/logout', {
                method: 'POST',
                credentials: 'include'
            });

            if (response.ok) {
                sessionStorage.removeItem('currentSession');
                navigate('/login', { replace: true });
            }
        } catch (error) {
            console.error('Logout failed:', error);
        }
    };

    return (
        <button onClick={handleLogout} className="logout-button">
            Logout
        </button>
    );
}