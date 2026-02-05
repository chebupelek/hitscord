import { Layout, Menu, Button } from 'antd';
import { UserOutlined, TeamOutlined, MessageOutlined, HistoryOutlined, LoginOutlined, LogoutOutlined, UserAddOutlined, DatabaseOutlined, CommentOutlined, } from '@ant-design/icons';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useSelector, useDispatch } from 'react-redux';
import { logoutThunkCreator } from '../../Reducers/HeaderReducer';

const { Sider } = Layout;

const SIDEBAR_WIDTH = 260;

function SideMenu() {
    const isAuth = useSelector(state => state.header.isAuth);
    const location = useLocation();
    const navigate = useNavigate();
    const dispatch = useDispatch();

    const handleLogout = () => {
        dispatch(logoutThunkCreator(navigate));
    };

    const handleLogin = () => {
        navigate('/login');
    };

    const HEADER_HEIGHT = 72;
    const FOOTER_HEIGHT = 88;

    return (
        <Sider width={SIDEBAR_WIDTH} style={{ background: '#1a3f76', position: 'fixed', left: 0, top: 0, height: '100vh'}}>
            <div style={{ height: HEADER_HEIGHT, padding: '16px', marginBottom: '20px', color: 'white', fontWeight: 'bold', fontSize: '24px', boxSizing: 'border-box' }} >
                Hitscord<br />Админпанель
            </div>
            <div style={{height: `calc(100vh - ${HEADER_HEIGHT + FOOTER_HEIGHT}px)`, overflowY: 'auto'}}>
                {isAuth && (
                    <Menu theme="dark" mode="inline" selectedKeys={[location.pathname]} inlineIndent={12} style={{ background: '#1a3f76', borderRight: 'none' }}
                        items={[
                            { key: '/users', icon: <UserOutlined />, label: <Link to="/users">Пользователи</Link> },
                            { key: '/roles', icon: <TeamOutlined />, label: <Link to="/roles">Роли</Link> },
                            { key: '/channels', icon: <MessageOutlined />, label: <Link to="/channels">Удаленные каналы</Link> },
                            { key: '/admin', icon: <UserAddOutlined />, label: <Link to="/admin">Добавление администратора</Link> },
                            { key: '/servers', icon: <DatabaseOutlined />, label: <Link to="/servers">Сервера</Link> },
                            //{ key: '/chats', icon: <CommentOutlined />, label: <Link to="/chats">Чаты</Link> },
                            { key: '/operations', icon: <HistoryOutlined />, label: <Link to="/operations">История операций</Link> },
                        ]}
                    />
                )}
            </div>
            <div style={{ height: FOOTER_HEIGHT, padding: '16px', boxSizing: 'border-box', background: '#1a3f76' }}>
                {isAuth ? (
                    <Button block icon={<LogoutOutlined />} onClick={handleLogout} style={{ backgroundColor: '#f0f0f0', borderColor: '#d9d9d9' }} >
                        Выход
                    </Button>
                ) : (
                    <Button block icon={<LoginOutlined />} onClick={handleLogin} style={{ backgroundColor: '#f0f0f0', borderColor: '#d9d9d9' }} >
                        Вход
                    </Button>
                )}
            </div>
        </Sider>
    );
}

export default SideMenu;
