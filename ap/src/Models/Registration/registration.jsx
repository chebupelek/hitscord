import { Card, Space, Input, Button, Typography } from "antd";
import { useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import { addRoleThunkCreator } from "../../Reducers/RegisterReducer";

const { Text } = Typography;

function Registration() 
{
    const [login, setLogin] = useState('');
    const [accountName, setAccountName] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');

    const [loginError, setLoginError] = useState('');
    const [accountNameError, setAccountNameError] = useState('');
    const [passwordError, setPasswordError] = useState('');
    const [confirmPasswordError, setConfirmPasswordError] = useState('');

    const dispatch = useDispatch();
    const navigate = useNavigate();
    const loading = useSelector(state => state.registration.loadingRegistration);

    const validate = () => {
        let valid = true;
        setLoginError('');
        setAccountNameError('');
        setPasswordError('');
        setConfirmPasswordError('');
        if (login.length < 10 || login.length > 50) 
        {
            setLoginError("Логин должен быть от 10 до 50 символов");
            valid = false;
        }
        if (accountName.length < 6 || accountName.length > 50)
        {
            setAccountNameError("Имя пользователя должно быть от 6 до 50 символов");
            valid = false;
        } 
        else if (!/^[a-zA-Zа-яА-ЯёЁ0-9 ]+$/.test(accountName)) 
        {
            setAccountNameError("Допустимы только буквы и цифры");
            valid = false;
        }
        if (password.length < 6) 
        {
            setPasswordError("Пароль должен быть не менее 6 символов");
            valid = false;
        }
        if (password !== confirmPassword) 
        {
            setConfirmPasswordError("Пароли не совпадают");
            valid = false;
        }
        return valid;
    };

    const handleRegister = () => {
        if (!validate())
        {
            return;
        }
        dispatch(addRoleThunkCreator({login, password, accountName}, navigate));
    };

    return (
        <Card style={{ width: '33%', marginTop: '10%', backgroundColor: '#f6f6fb' }}>
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <h1>Добавление администратора</h1>
                <div>
                    <span>Логин</span>
                    <Input
                        value={login}
                        status={loginError ? "error" : ""}
                        onChange={e => {
                            setLogin(e.target.value);
                            setLoginError('');
                        }}
                        placeholder="admin.login@example"
                    />
                    {loginError && <Text type="danger">{loginError}</Text>}
                </div>
                <div>
                    <span>Имя пользователя</span>
                    <Input
                        value={accountName}
                        status={accountNameError ? "error" : ""}
                        onChange={e => {
                            setAccountName(e.target.value);
                            setAccountNameError('');
                        }}
                        placeholder="Иван Иванов"
                    />
                    {accountNameError && <Text type="danger">{accountNameError}</Text>}
                </div>
                <div>
                    <span>Пароль</span>
                    <Input.Password
                        value={password}
                        status={passwordError ? "error" : ""}
                        onChange={e => {
                            setPassword(e.target.value);
                            setPasswordError('');
                        }}
                    />
                    {passwordError && <Text type="danger">{passwordError}</Text>}
                </div>
                <div>
                    <span>Подтверждение пароля</span>
                    <Input.Password
                        value={confirmPassword}
                        status={confirmPasswordError ? "error" : ""}
                        onChange={e => {
                            setConfirmPassword(e.target.value);
                            setConfirmPasswordError('');
                        }}
                    />
                    {confirmPasswordError && (
                        <Text type="danger">{confirmPasswordError}</Text>
                    )}
                </div>
                <Button type="primary" loading={loading} style={{ width: '100%', backgroundColor: '#317cb9' }} onClick={handleRegister}>Добавить администратора</Button>
            </Space>
        </Card>
    );
}

export default Registration;
