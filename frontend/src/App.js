import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import RegisterPage from './components/RegisterPage';
import './App.css';
import LoginPage from "./components/LoginPage";
import ChatsPage from "./components/ChatsPage";

function App() {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<Navigate to="/register" replace />} />
                <Route path="/register" element={<RegisterPage/>}/>
                <Route path="/login" element={<LoginPage/>}/>
                <Route path="/chats" element={<ChatsPage/>}/>
            </Routes>
        </Router>
    );
}

export default App;