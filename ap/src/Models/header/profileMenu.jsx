import { Dropdown } from 'antd';
import { useDispatch, useSelector } from 'react-redux';
import { DownOutlined } from '@ant-design/icons';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Link } from 'react-router-dom';
import { logoutThunkCreator } from '../../Reducers/HeaderReducer';

const getMaxWidth = (width) => {
    if (width < 500) {
      return '50px';
    } else if (width < 750) {
      return '75px';
    } else if (width < 1000) {
      return '100px';
    } else {
      return '150px';
    }
};

function ProfileMenu() {
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const isAuth = useSelector(state => state.header.isAuth);
    const [windowWidth, setWindowWidth] = useState(window.innerWidth);

    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener('resize', handleResize);
        return () => window.removeEventListener('resize', handleResize);
    }, []);

    const handleLogout = () => {
        dispatch(logoutThunkCreator(navigate));
    }
    
    const items = [
        {
            key: 'logout',
            label: (
                <span onClick={handleLogout}>Выход</span>
            ),
        }
    ];

    const textStyle = {
        display: 'inline-block',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        maxWidth: getMaxWidth(windowWidth),
        color: 'white',
        verticalAlign: 'middle'
    };

    return (
        <div style={{ marginLeft: 'auto'}}>
            {isAuth ? (
                <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
                    <a onClick={(e) => e.preventDefault()} style={{ display: 'inline-flex', alignItems: 'center', justifyItems: 'end' }}>
                        <span style={textStyle}>Меню</span>
                        <DownOutlined style={{ color: 'white'}} />
                    </a>
                </Dropdown>
            ) : (
                <Link to="/login" style={{ color: 'white' }}>Вход</Link>
            )}
        </div>
    );
}

export default ProfileMenu;