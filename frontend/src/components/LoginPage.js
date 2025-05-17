import React, {useState} from 'react';
import {Link, useNavigate} from 'react-router-dom';
import './LoginPage.css';

function LoginPage() {
    const [formData, setFormData] = useState({
        email: '',
        password: '',
    });

    const [errors, setErrors] = useState({});
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();

    const validate = () => {
        const newErrors = {};
        if (!formData.email.includes('@')) newErrors.email = 'Invalid email';
        if (formData.password.length < 6) newErrors.password = 'Password must be at least 6 characters';
        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!validate()) return;

        setIsLoading(true);
        try {
            const response = await fetch('http://localhost:5079/api/auth/login', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify(formData),
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Login failed');
            }

            navigate('/chats');
        } catch (error) {
            setErrors({api: error.message});
        } finally {
            setIsLoading(false);
        }
    };

    const handleChange = (e) => {
        setFormData({...formData, [e.target.name]: e.target.value});
    };

    return (
        <div className="home-container">
            <div className="auth-container">
                <div className="login-container">
                    <h1>Login</h1>
                    <form onSubmit={handleSubmit}>
                        <div className="input-group">
                            <label>Email</label>
                            <input
                                type="email"
                                name="email"
                                value={formData.email}
                                onChange={handleChange}
                            />
                            {errors.email && <span className="error">{errors.email}</span>}
                        </div>
                        <div className="input-group">
                            <label>Password</label>
                            <input
                                type="password"
                                name="password"
                                value={formData.password}
                                onChange={handleChange}
                                placeholder="Enter your password"
                                disabled={isLoading}
                            />
                            {errors.email && <span className="error">{errors.email}</span>}
                        </div>
                        <button type="submit" className="submit-button">
                            {isLoading ? <span className="Loader..."></span> : "Login"}
                        </button>
                    </form>
                    <p>
                        Don't account? <Link to="/register">Sign up</Link>
                    </p>
                </div>

                <div className="info-container">
                    <div>
                        <h2>Welcome to the Telegram!</h2>
                    </div>
                </div>
            </div>

            <div className="button-container">
                <Link to="/">
                    <button className="action-button">Back</button>
                </Link>
            </div>
        </div>
    );
}

export default LoginPage;