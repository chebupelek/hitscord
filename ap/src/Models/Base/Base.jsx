import { Route, Routes, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Layout } from 'antd';
import { useSelector } from 'react-redux';

import Login from '../Login/Login';
import Users from '../Users/users';
import Channels from '../Channels/channels';
import Roles from '../Roles/roles';
import Operations from '../Operations/operations';
import Registration from '../Registration/registration';
import Servers from '../Servers/servers';
import ServerInfoPage from '../ServerData/serverData'

function Base() {
    const location = useLocation();
    const navigate = useNavigate();

    const isAuth = useSelector(state => state.header.isAuth);

    return (
        <Layout.Content style={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
            <Routes>
                <Route path="/" element={isAuth ? <Navigate to="/users" /> : <Navigate to="/login" />} />
                <Route path="/login" element={<Login />} />
                <Route path="/users" element={<Users />} />
                <Route path="/roles" element={<Roles />} />
                <Route path='/channels' element={<Channels />} />
                <Route path='/admin' element={<Registration />} />
                <Route path='/servers' element={<Servers />} />
                <Route path='/chats' element={<Operations />} />
                <Route path='/operations' element={<Operations />} />
                <Route path='/server/:id' element={<ServerInfoPage />} />
            </Routes>
        </Layout.Content>
    );
}

export default Base;
