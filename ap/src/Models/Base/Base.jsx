import { Route, Routes, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import { Layout } from 'antd';

import Login from '../Login/Login';
import Users from '../Users/users';
import Channels from '../Channels/channels';
import Roles from '../Roles/roles';

function Base() {
    const location = useLocation();
    const navigate = useNavigate();

    useEffect(() => {
        console.log('Navigating to:', location.pathname);
    }, [location]);

    return (
        <Layout.Content style={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
            <Routes>
                <Route path="/" element={localStorage.getItem('token') ? <Navigate to="/users" /> : <Navigate to="/login" />} />
                <Route path="/login" element={<Login />} />
                <Route path="/users" element={<Users />} />
                <Route path="/roles" element={<Roles />} />
                <Route path='/channels' element={<Channels />} />
            </Routes>
        </Layout.Content>
    );
}

export default Base;
