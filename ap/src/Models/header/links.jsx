import { useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { Space } from 'antd';

function Links() {
    const stateLog = useSelector(state => state.header.isAuth);
    
    return (
        <>
            {(stateLog) ? (
                <Space direction='horizontal' size={'large'}>
                    <Link to="/users" style={{ color: 'white'}}>Пользователи</Link>
                    <Link to="/roles" style={{ color: 'white'}}>Роли</Link>
                    <Link to="/channels" style={{ color: 'white' }}>Каналы</Link>
                </Space>
            ) : (
                <></>
            )}
        </>
    );
}

export default Links;