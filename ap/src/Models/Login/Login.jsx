import React, { useState } from 'react';
import { useDispatch } from 'react-redux';
import { Card, Space, Input, Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import { loginThunkCreator } from '../../Reducers/HeaderReducer';

function Login(){
    const [login, setLogin] = useState('');
    const [password, setPassword] = useState('');
    const dispatch = useDispatch();
    const navigate = useNavigate();

    const handleLogin = () => {
        if (password.length < 1) 
        {
            alert("Пароль должен быть заполнен");
            return;
        }

        const loginData = 
        {
            login: login,
            password: password
        };

        dispatch(loginThunkCreator(loginData, navigate));
    };

    return(
        <Card style={{ width: '33%', boxSizing: 'border-box', marginTop: '10%', backgroundColor: '#f6f6fb', boxShadow: '0 0 8px rgba(0, 0, 0, 0.3)' }}>
            <Space direction='vertical' size='middle' style={{ width: '100%' }}>
                <h1>Вход</h1>
                <div style={{ width: '100%' }}>
                    <span>Login</span>
                    <Input style={{ width: '100%' }} value={login} onChange={e => setLogin(e.target.value)} placeholder="Marina Evgenyevna"/>
                </div>
                <div style={{ width: '100%' }}>
                    <span>Пароль</span>
                    <Input type="password" style={{ width: '100%' }} value={password} onChange={e => setPassword(e.target.value)}/>
                </div>
                <div>
                    <Space direction='vertical' size='small' style={{ width: '100%' }}>
                        <Button type="primary" style={{ width: '100%', backgroundColor: '#317cb9' }} onClick={handleLogin}>Войти</Button>
                    </Space>
                </div>
            </Space>
        </Card>
    );
}

export default Login;