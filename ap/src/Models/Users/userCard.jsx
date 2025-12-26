import { Card, Typography, Checkbox, Tag, Avatar, Spin, Space, Button, Modal, Input, notification } from "antd";
import { useState, useEffect } from 'react';
import { CloseOutlined, UserOutlined, DeleteOutlined, KeyOutlined } from "@ant-design/icons";
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate } from "react-router-dom";
import { removeRoleThunkCreator, getIconThunkCreator, changePasswordThunkCreator } from "../../Reducers/UsersListReducer";
import styles from "./UserCard.module.css";
import { PlusOutlined } from "@ant-design/icons";
import { Popover, Select } from "antd";
import { addRoleThunkCreator, getRolesShortListThunkCreator } from "../../Reducers/UsersListReducer";


function formatDate(dateString) 
{
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

function RoleTag({ role, onRemove }) 
{
    const handleClick = (e) => { e.stopPropagation(); onRemove(role.id); };
    const handleClose = (e) => { e.stopPropagation(); onRemove(role.id); };
    return (
        <Tag className={styles.roleTag} key={role.id} color="blue" onClick={handleClick} closable onClose={handleClose} closeIcon={<CloseOutlined style={{ fontSize: "12px", color: "red" }} onClick={(e) => e.stopPropagation()} />}>
            {role.name}
        </Tag>
    );
}

function UserCard({ id, name, mail, accountTag, icon, accountCreateDate, systemRoles = [], checked, onCheck, reloadUsers }) 
{
    const dispatch = useDispatch();
    const navigate = useNavigate();

    const [iconSrc, setIconSrc] = useState(null);
    const [iconLoading, setIconLoading] = useState(false);

    const [isModalVisible, setIsModalVisible] = useState(false);
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isConfirmVisible, setIsConfirmVisible] = useState(false);
    const [passwordError, setPasswordError] = useState('');
    const [confirmPasswordError, setConfirmPasswordError] = useState('');

    const rolesShort = useSelector(state => state.users.roles) || [];
    const [selectedRoleId, setSelectedRoleId] = useState(null);
    const [addRoleLoading, setAddRoleLoading] = useState(false);
    const [popoverOpen, setPopoverOpen] = useState(false);

    useEffect(() => {
        if (!icon?.fileId) 
        {
            setIconSrc(null);
            setIconLoading(false);
            return;
        }
        setIconLoading(true);
        dispatch(getIconThunkCreator(icon.fileId, navigate))
            .then(data => {
                if (!data)
                {
                    return;
                }
                setIconSrc(`data:${data.fileType};base64,${data.base64File}`);
            })
            .finally(() => setIconLoading(false));
    }, [icon?.fileId, navigate, dispatch]);

    const handleRemoveRole = (roleId) => {
        const payload = { roleId, userId: id };
        dispatch(removeRoleThunkCreator(payload, navigate, reloadUsers));
    };

    const handleCardClick = (e) => {
        if (!e.target.closest(`.${styles.roleTag}`)) 
        {
            onCheck(id, !checked);
        }
    };

    const renderRoles = () => (
        <>
            {systemRoles.map(role => (
                <RoleTag key={role.id} role={role} onRemove={handleRemoveRole} />
            ))}
            <Popover content={addRolePopoverContent} trigger="click" open={popoverOpen} onOpenChange={setPopoverOpen}>
                <Tag className={styles.roleTag} style={{ cursor: "pointer", borderStyle: "dashed", color: "#1677ff" }} icon={<PlusOutlined />} onClick={e => e.stopPropagation()}>
                    Добавить
                </Tag>
            </Popover>
        </>
    );

    const openModal = (e) => {
        e.stopPropagation();
        setPassword('');
        setConfirmPassword('');
        setIsConfirmVisible(false);
        setIsModalVisible(true);
    };

    const handleCancel = () => {
        setIsModalVisible(false);
        setIsConfirmVisible(false);
    };

    const handleNextStep = () => {
        let hasError = false;
        if (!password) 
        {
            setPasswordError("Необходимо ввести пароль");
            hasError = true;
        } 
        else if (password.length < 6) 
        {
            setPasswordError("Пароль должен быть не меньше 6 символов");
            hasError = true;
        } 
        else 
        {
            setPasswordError('');
        }

        if (!confirmPassword) 
        {
            setConfirmPasswordError("Необходимо подтвердить пароль");
            hasError = true;
        } 
        else if (password !== confirmPassword) 
        {
            setConfirmPasswordError("Пароли не совпадают");
            hasError = true;
        } 
        else 
        {
            setConfirmPasswordError('');
        }

        if (!hasError) 
        {
            setIsConfirmVisible(true);
        }
    };

    const handleChangePassword = () => {
        dispatch(changePasswordThunkCreator({ UserId: id, Password: password }, navigate, reloadUsers));
        setIsModalVisible(false);
        setIsConfirmVisible(false);
    };

    const fetchRoles = (query = "name=") => {
        dispatch(getRolesShortListThunkCreator(query, navigate));
    };

    const handleAddRole = () => {
        if (!selectedRoleId)
        {
            return;
        }

        setAddRoleLoading(true);

        const payload = {
            roleId: selectedRoleId,
            usersIds: [id],
        };

        dispatch(addRoleThunkCreator(
            payload,
            navigate,
            () => { setSelectedRoleId(null); },
            () => { setPopoverOpen(false); },
            reloadUsers
        ));
    };

    const addRolePopoverContent = (
        <div onClick={e => e.stopPropagation()}>
            <Space direction="vertical" size="small" style={{ width: 200 }}>
                <Select showSearch placeholder="Выберите роль" value={selectedRoleId} onFocus={() => fetchRoles()} onSearch={value => fetchRoles(`name=${value}`)} onChange={setSelectedRoleId} filterOption={false} style={{ width: "100%" }} >
                    {rolesShort.map(role => (
                        <Select.Option key={role.id} value={role.id}>
                            {role.name}
                        </Select.Option>
                    ))}
                </Select>
                <Button type="primary" size="small" block disabled={!selectedRoleId} loading={addRoleLoading} onClick={handleAddRole}>Добавить</Button>
            </Space>
        </div>
    );

    return (
        <>
            <Card className={styles.userCard} onClick={handleCardClick}>
                <div className={styles.cardContent}>
                    <Avatar
                        src={!iconLoading ? iconSrc : null}
                        icon={iconLoading ? <Spin size="small" /> : !iconSrc && <UserOutlined />}
                        className={styles.userAvatar}
                    />
                    <div className={styles.textBlock}>
                        <Checkbox checked={checked} style={{ pointerEvents: 'none' }}>
                            <Typography.Title level={4} style={{ margin: 0 }}>{name}</Typography.Title>
                        </Checkbox>
                        <div>Почта - <strong>{mail}</strong></div>
                        <div>Тэг - <strong>{accountTag}</strong></div>
                        <div>Дата создания аккаунта - <strong>{formatDate(accountCreateDate)}</strong></div>
                        <div style={{ marginBottom: "6px" }}>Роли:</div>
                        <div className={styles.roles}>{renderRoles()}</div>
                        <Space className={styles.userCardButtons} style={{ marginTop: '12px' }}>
                            <Button type="primary" danger onClick={(e) => { e.stopPropagation(); }} icon={<DeleteOutlined />}>Удалить пользователя</Button>
                            <Button type="default" onClick={openModal} icon={<KeyOutlined />}>Сменить пароль</Button>
                        </Space>
                    </div>
                </div>
            </Card>
            <Modal
                title={isConfirmVisible ? "Подтверждение изменения пароля" : "Новый пароль"}
                visible={isModalVisible}
                onCancel={handleCancel}
                okText={isConfirmVisible ? "Подтвердить" : "Далее"}
                cancelText="Отмена"
                onOk={isConfirmVisible ? handleChangePassword : handleNextStep}
            >
                {!isConfirmVisible ? (
                    <>
                        <Input.Password placeholder="Введите новый пароль" value={password} onChange={(e) => setPassword(e.target.value)} style={{ marginBottom: 4 }}/>
                        {passwordError && <Typography.Text type="danger">{passwordError}</Typography.Text>}
                        <Input.Password placeholder="Повторите новый пароль" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} style={{ marginBottom: 4 }}/>
                        {confirmPasswordError && <Typography.Text type="danger">{confirmPasswordError}</Typography.Text>}
                    </>
                ) : (
                    <Typography.Text>Вы уверены, что хотите изменить пароль пользователя <strong>{name}</strong>?</Typography.Text>
                )}
            </Modal>
        </>
    );
}

export default UserCard;
