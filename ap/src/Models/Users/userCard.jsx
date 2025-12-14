import { Card, Typography, Checkbox, Tag, Avatar, Spin  } from "antd";
import { useState, useEffect } from 'react';
import { CloseOutlined, UserOutlined  } from "@ant-design/icons";
import { useDispatch } from "react-redux";
import { useNavigate } from "react-router-dom";
import { removeRoleThunkCreator, getIconThunkCreator } from "../../Reducers/UsersListReducer";
import styles from "./UserCard.module.css";

function formatDate(dateString) 
{
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

function RoleTag({ role, onRemove }) {
    const handleClick = (e) => {
        e.stopPropagation();
        onRemove(role.id);
    };

    const handleClose = (e) => {
        e.stopPropagation();
        onRemove(role.id);
    };

    return (
        <Tag
            className={styles.roleTag}
            key={role.id}
            color="blue"
            onClick={handleClick}
            closable
            onClose={handleClose}
            closeIcon={<CloseOutlined style={{ fontSize: "12px", color: "red" }} onClick={(e) => e.stopPropagation()} />}
        >
            {role.name}
        </Tag>
    );
}


function UserCard({ id, name, mail, accountTag, icon, accountCreateDate, systemRoles = [], checked, onCheck, reloadUsers }) {
    const dispatch = useDispatch();
    const navigate = useNavigate();

    const [iconSrc, setIconSrc] = useState(null);
    const [iconLoading, setIconLoading] = useState(false);

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
            .finally(() => {
                setIconLoading(false);
            });
    }, [icon?.fileId, navigate, dispatch]);

    const handleRemoveRole = (roleId) => {
        const payload = { roleId, userId: id };
        dispatch(removeRoleThunkCreator(payload, navigate, reloadUsers));
    };

    const handleCardClick = (e) => {
        if (!e.target.closest(`.${styles.roleTag}`)) {
            onCheck(id, !checked);
        }
    };

    const renderRoles = () => systemRoles.map(role => <RoleTag key={role.id} role={role} onRemove={handleRemoveRole} />);

    return (
        <Card className={styles.userCard} onClick={handleCardClick}>
            <div className={styles.cardContent}>
                <Avatar src={!iconLoading ? iconSrc : null} icon={iconLoading ? <Spin size="small" /> : !iconSrc && <UserOutlined />} className={styles.userAvatar}/>
                <div className={styles.textBlock}>
                    <Checkbox checked={checked} style={{ pointerEvents: 'none' }}>
                        <Typography.Title level={4} style={{ margin: 0 }}>
                            {name}
                        </Typography.Title>
                    </Checkbox>
                    <div>Почта - <strong>{mail}</strong></div>
                    <div>Тэг - <strong>{accountTag}</strong></div>
                    <div>Дата создания аккаунта - <strong>{formatDate(accountCreateDate)}</strong></div>
                    <div style={{ marginBottom: "6px" }}>Роли:</div>
                    <div className={styles.roles}>{renderRoles()}</div>
                </div>
            </div>
        </Card>
    );
}

export default UserCard;
