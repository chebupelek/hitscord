import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { Spin, Card, Typography, Avatar, Tag, Space, Divider, Button, Collapse, Popover, Modal, Input, Select, Switch, message, InputNumber, ColorPicker } from "antd";
import { PlusOutlined, MoreOutlined, UserOutlined, DatabaseOutlined, TeamOutlined, DeleteOutlined, EditOutlined, UploadOutlined, CloseOutlined } from "@ant-design/icons";
import { getServerDataThunkCreator, clearServerDataActionCreator } from "../../Reducers/ServersReducer";
import { getIconThunkCreator } from "../../Reducers/UsersListReducer";

const { Title, Text } = Typography;
const { Panel } = Collapse;

const ALL_PANELS = ["roles", "users", "channels", "presets"];

const PERMISSION_LABELS = {
    canChangeRole: "Изменять роли",
    canWorkChannels: "Управлять каналами",
    canDeleteUsers: "Удалять пользователей",
    canMuteOther: "Мутить пользователей",
    canDeleteOthersMessages: "Удалять сообщения",
    canIgnoreMaxCount: "Игнорировать лимиты",
    canCreateRoles: "Создавать роли",
    canCreateLessons: "Создавать занятия",
    canCheckAttendance: "Проверять посещаемость"
};

function getContrastTextColor(hex) 
{
    if(!hex)
    {
        return "#000";
    }

    const c = hex.replace("#", "");
    const r = parseInt(c.substring(0, 2), 16);
    const g = parseInt(c.substring(2, 4), 16);
    const b = parseInt(c.substring(4, 6), 16);

    const brightness = (r * 299 + g * 587 + b * 114) / 1000;

    return brightness > 140 ? "#000000" : "#ffffff";
}

export default function ServerInfoPage() 
{
    const { id } = useParams();
    const dispatch = useDispatch();
    const serverData = useSelector(s => s.servers.serverData);
    const loading = useSelector(s => s.servers.loadingServerData);

    const [iconSrc, setIconSrc] = useState(null);
    const [iconLoading, setIconLoading] = useState(false);
    const [activePanels, setActivePanels] = useState(ALL_PANELS);

    const [editModalVisible, setEditModalVisible] = useState(false);
    const [iconModalVisible, setIconModalVisible] = useState(false);
    const [deleteModalVisible, setDeleteModalVisible] = useState(false);

    const [newName, setNewName] = useState("");
    const [newType, setNewType] = useState(0);
    const [isPrivate, setIsPrivate] = useState(false);

    const [selectedCreator, setSelectedCreator] = useState(null);

    const [addRoleVisible, setAddRoleVisible] = useState(false);
    const [roleName, setRoleName] = useState("");
    const [roleColor, setRoleColor] = useState("#ffffff");

    const [addPresetVisible, setAddPresetVisible] = useState(false);
    const [presetRoleId, setPresetRoleId] = useState(null);
    const [presetSystemRoleId, setPresetSystemRoleId] = useState(null);

    const [addChannelVisible, setAddChannelVisible] = useState(false);
    const [channelName, setChannelName] = useState("");
    const [channelType, setChannelType] = useState("Text");
    const [maxUsers, setMaxUsers] = useState(2);

    useEffect(() => {
        if (serverData?.users?.length) 
        {
            const creatorUser = serverData.users.find(u => u.roles.some(r => r.roleType === 0));
            setSelectedCreator(creatorUser || null);
        }
    }, [serverData]);

    useEffect(() => {
        if(id) 
        {
            dispatch(getServerDataThunkCreator(`ServerId=${id}`, null));
        }
        return () => dispatch(clearServerDataActionCreator());
    }, [id, dispatch]);

    useEffect(() => {
        if(!serverData?.icon?.fileId)
        {
            return;
        }

        setIconLoading(true);
        dispatch(getIconThunkCreator(serverData.icon.fileId, null))
            .then(d => d && setIconSrc(`data:${d.fileType};base64,${d.base64File}`))
            .finally(() => setIconLoading(false));
    }, [serverData?.icon?.fileId, dispatch]);

    useEffect(() => {
        if(serverData) 
        {
            setNewName(serverData.serverName);
            setNewType(serverData.serverType);
            setIsPrivate(serverData.isPrivate ?? false);
        }
    }, [serverData]);

    if(loading || !serverData)
    {
        return <Spin style={{ marginTop: "20%" }} size="large" />;
    }

    const usersByRole = role => serverData.users.filter(u => u.roles.some(r => r.roleName === role.name));

    const openEditModal = () => setEditModalVisible(true);
    const closeEditModal = () => setEditModalVisible(false);

    const openIconModal = () => setIconModalVisible(true);
    const closeIconModal = () => setIconModalVisible(false);

    const openDeleteModal = () => setDeleteModalVisible(true);
    const closeDeleteModal = () => setDeleteModalVisible(false);

    const handleIconChange = () => {
        message.info("Функция смены иконки пока не подключена");
    };
    const handleIconRemove = () => {
        setIconSrc(null);
        message.info("Иконка удалена (локально)");
    };
    const handleSaveChanges = () => {
        message.success("Изменения сохранены (пока только локально)");
        closeEditModal();
    };
    const handleDeleteServer = () => {
        message.success("Сервер удален (пока только локально)");
        closeDeleteModal();
    };

    const panelHeader = (title, onAdd) => (
        <Space style={{ width: "100%", justifyContent: "space-between" }}>
            <span>{title}</span>
            <Button size="small" type="text" icon={<PlusOutlined />} onClick={e => {e.stopPropagation(); onAdd();}}/>
        </Space>
    );


    return (
        <div style={{ width: "85%", margin: "20px auto" }}>
            <Card>
                <Space style={{ width: "100%", justifyContent: "space-between", alignItems: "center" }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                        <div style={{ position: "relative", display: "inline-block" }}>
                            <Avatar
                                size={64} src={!iconLoading ? iconSrc : null} icon={<DatabaseOutlined />} style={{ cursor: "pointer", transition: "0.2s", filter: "brightness(100%)" }}
                                onMouseEnter={e => e.currentTarget.style.filter = "brightness(70%)"} onMouseLeave={e => e.currentTarget.style.filter = "brightness(100%)"} onClick={openIconModal}
                            />
                            <div style={{ position: "absolute", top: 0, left: 0, width: 64, height: 64, display: "flex", alignItems: "center", justifyContent: "center", pointerEvents: "none"}}>
                                <MoreOutlined style={{ color: "white", fontSize: 24 }} />
                            </div>
                        </div>

                        <div style={{ display: "flex", flexDirection: "column" }}>
                            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                <Title level={3} style={{ margin: 0 }}>{serverData.serverName}</Title>
                                <Button type="text" icon={<EditOutlined />} onClick={openEditModal}/>
                            </div>

                            <Space style={{ marginTop: 8 }}>
                                <Tag>{serverData.serverType === 0 ? "Студенческий" : "Учебный"}</Tag>
                                <Tag icon={<TeamOutlined />}>{serverData.users.length}</Tag>
                                <Tag color={serverData.isPrivate ? "red" : "green"}>{serverData.isPrivate ? "Закрытый" : "Открытый"}</Tag>
                            </Space>
                        </div>
                    </div>

                    <Button onClick={() => setActivePanels(activePanels.length ? [] : ALL_PANELS)}>{activePanels.length ? "Свернуть всё" : "Развернуть всё"}</Button>
                </Space>

                <Divider />

                <Collapse activeKey={activePanels} onChange={setActivePanels}>
                    {serverData.roles.length > 0 && (
                        <Panel header={panelHeader("Роли", () => setAddRoleVisible(true))} key="roles">
                            {serverData.roles.map(role => (
                                <ServerRoleCard key={role.name} role={role} usersByRole={usersByRole} />
                            ))}
                        </Panel>
                    )}

                    {serverData.serverType === 1 && serverData.presets.length > 0 && (
                        <Panel header={panelHeader("Пресеты", () => setAddPresetVisible(true))} key="presets">
                            <Space direction="vertical" style={{ width: "100%" }}>
                                {serverData.presets.map(preset => (
                                    <ServerPresetCard
                                        key={`${preset.serverRoleId}-${preset.systemRoleId}`}
                                        preset={preset}
                                    />
                                ))}
                            </Space>
                        </Panel>

                    )}

                    {serverData.users.length > 0 && (
                        <Panel header="Пользователи" key="users">
                            {serverData.users.map(user => (
                                <ServerUserCard key={user.userId} user={user} roles={serverData.roles} serverData={serverData} />
                            ))}
                        </Panel>
                    )}

                    {(serverData.channels.textChannels.length > 0 || serverData.channels.voiceChannels.length > 0 || serverData.channels.notificationChannels.length > 0 || serverData.channels.pairVoiceChannels.length > 0) && (
                            <Panel header={panelHeader("Каналы", () => setAddChannelVisible(true))} key="channels">
                                {[
                                    ...serverData.channels.textChannels.map(c => ({ ...c, channelKey: "textChannels" })),
                                    ...serverData.channels.voiceChannels.map(c => ({ ...c, channelKey: "voiceChannels" })),
                                    ...serverData.channels.notificationChannels.map(c => ({ ...c, channelKey: "notificationChannels" })),
                                    ...serverData.channels.pairVoiceChannels.map(c => ({ ...c, channelKey: "pairVoiceChannels" }))
                                ].map(channel => (
                                    <ServerChannelCard key={channel.channelId} channel={channel} roles={serverData.roles} channelKey={channel.channelKey}/>
                                ))}
                            </Panel>
                    )}
                </Collapse>
            </Card>

            <Modal title="Редактировать сервер" open={editModalVisible} onCancel={closeEditModal} okText="Сохранить" cancelText="Отмена"
                onOk={handleSaveChanges} okButtonProps={{disabled: !newName || newName.length < 6 || newName.length > 50}}
                footer={[
                    <Button key="delete" danger icon={<DeleteOutlined />} onClick={openDeleteModal}>Удалить</Button>,
                    <Button key="cancel" onClick={closeEditModal}>Отмена</Button>,
                    <Button key="save" type="primary" onClick={handleSaveChanges} disabled={!newName || newName.length < 6 || newName.length > 50 || !selectedCreator}>Сохранить</Button>
                ]}
            >
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Text strong>Название сервера:</Text>
                    <Input value={newName} onChange={e => setNewName(e.target.value)} />

                    <Text strong>Тип сервера:</Text>
                    <Select value={newType} onChange={setNewType} style={{ width: "100%" }}>
                        <Select.Option value={0}>Студенческий</Select.Option>
                        <Select.Option value={1}>Учебный</Select.Option>
                    </Select>

                    <Text strong>Создатель сервера:</Text>
                    <Select value={selectedCreator?.userId}
                        onChange={userId => {
                            const user = serverData.users.find(u => u.userId === userId);
                            setSelectedCreator(user);
                        }}
                        style={{ width: "100%" }} placeholder="Выберите создателя"
                    >
                        {serverData.users.filter(u => u.roles.some(r => r.roleName === "Создатель")).map(u => (<Select.Option key={u.userId} value={u.userId}>{u.userName}</Select.Option>))}
                    </Select>

                    <Text strong>Открытость сервера:</Text>
                    <Switch
                        checked={!isPrivate}
                        onChange={checked => setIsPrivate(!checked)}
                        checkedChildren="Открыт"
                        unCheckedChildren="Закрыт"
                    />
                </Space>
            </Modal>


            <Modal title="Иконка сервера" open={iconModalVisible} onCancel={closeIconModal} footer={null}>
                <Space direction="vertical" style={{ width: "100%", alignItems: "center" }}>
                    <Avatar size={128} src={iconSrc} icon={<DatabaseOutlined />} />
                    <Space>
                        <Button icon={<UploadOutlined />} onClick={handleIconChange}>Заменить</Button>
                        <Button icon={<CloseOutlined />} onClick={handleIconRemove} danger>Удалить</Button>
                    </Space>
                </Space>
            </Modal>

            <Modal title="Удалить сервер" open={deleteModalVisible} onCancel={closeDeleteModal} okText="Удалить" okButtonProps={{ danger: true }} onOk={handleDeleteServer}>
                <Text>Вы уверены, что хотите удалить сервер <strong>{serverData.serverName}</strong>?</Text>
            </Modal>

            <Modal title="Добавить роль" open={addRoleVisible} onCancel={() => setAddRoleVisible(false)}
                onOk={() => {
                    message.success("Роль добавлена");
                    setAddRoleVisible(false);
                    setRoleName("");
                    setRoleColor("#ffffff");
                }}
                okButtonProps={{disabled: roleName.length < 1 || roleName.length > 100 || !/^#[0-9A-Fa-f]{6}$/.test(roleColor)}}
            >
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Text strong>Название роли</Text>
                    <Input style={{width: "100%"}} value={roleName} onChange={e => setRoleName(e.target.value)} maxLength={100}/>
                    <Text strong>Цвет роли</Text>
                    <ColorPicker style={{width: "100%"}} value={roleColor} onChange={color => setRoleColor(color.toHexString())} showText
                        presets={[
                            {
                                label: "Стандартные",
                                colors: [
                                    "#ffffff",
                                    "#000000",
                                    "#f5222d",
                                    "#fa8c16",
                                    "#fadb14",
                                    "#52c41a",
                                    "#1677ff",
                                    "#722ed1"
                                ]
                            }
                        ]}
                    />
                </Space>
            </Modal>

            <Modal title="Добавить пресет" open={addPresetVisible} onCancel={() => setAddPresetVisible(false)}
                onOk={() => {
                    message.success("Пресет добавлен (локально)");
                    setAddPresetVisible(false);
                    setPresetRoleId(null);
                    setPresetSystemRoleId(null);
                }}
                okButtonProps={{ disabled: !presetRoleId }}
            >
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Text strong>Роль сервера</Text>
                    <Select value={presetRoleId} onChange={setPresetRoleId} style={{width: "100%"}} placeholder="Выберите роль">
                        {serverData.roles.map(role => (<Select.Option key={role.id} value={role.id}>{role.name}</Select.Option>))}
                    </Select>
                    <Text strong>Системная роль</Text>
                    <Select value={presetSystemRoleId} placeholder="Пока не реализовано" disabled style={{width: "100%"}}/>
                </Space>
            </Modal>

            <Modal title="Добавить канал" open={addChannelVisible} onCancel={() => setAddChannelVisible(false)}
                onOk={() => {
                    message.success("Канал добавлен (локально)");
                    setAddChannelVisible(false);
                    setChannelName("");
                    setChannelType("Text");
                    setMaxUsers(2);
                }}
                okButtonProps={{disabled: channelName.length < 1 || channelName.length > 100 || (channelType === "Voice" && (maxUsers < 2 || maxUsers > 999))}}
            >
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Text strong>Название канала</Text>
                    <Input value={channelName} onChange={e => setChannelName(e.target.value)} maxLength={100} style={{width: "100%"}}/>
                    <Text strong>Тип канала</Text>
                    <Select value={channelType} onChange={setChannelType} style={{width: "100%"}}>
                        <Select.Option value="Text">Текстовый</Select.Option>
                        <Select.Option value="Voice">Голосовой</Select.Option>
                        <Select.Option value="Notification">Уведомления</Select.Option>
                    </Select>
                    {channelType === "Voice" && (
                        <>
                            <Text strong>Максимум пользователей</Text>
                            <InputNumber min={2} max={99} value={maxUsers} onChange={setMaxUsers} style={{ width: "100%" }}/>
                        </>
                    )}
                </Space>
            </Modal>
        </div>
    );
}

function ServerRoleCard({ role, usersByRole }) 
{
    const [editVisible, setEditVisible] = useState(false);
    const [editName, setEditName] = useState(role.name);
    const [editColor, setEditColor] = useState(role.color);

    const globalPerms = Object.entries(role.permissions).filter(([, v]) => v);

    const channelRights = [
        ["Может видеть", role.channelCanSee],
        ["Может писать", role.channelCanWrite],
        ["Может создавать подканалы", role.channelCanWriteSub],
        ["Получает обязательные уведомления", role.channelNotificated],
        ["Может присоединяться", role.channelCanJoin],
        ["Может пользоваться", role.channelCanUse]
    ].filter(([, arr]) => arr.length);

    const textColor = getContrastTextColor(role.color);
    const users = usersByRole(role);

    const handleDeleteRole = () => {
        console.log("Удалить роль:", role.name);
    };

    const handleRemoveGlobalPerm = permKey => {
        console.log("Удалить глобальное право:", permKey);
    };

    const handleAddGlobalPerm = permKey => {
        console.log("Добавить глобальное право:", permKey);
    };

    return (
        <Card size="small" style={{ marginBottom: 12 }}>
            <Space direction="vertical" style={{ width: "100%" }} size={4}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%" }}>
                    <Space align="center">
                        <Tag color={role.color} style={{ color: textColor, fontWeight: 500, padding: "4px 10px" }}>{role.name}</Tag>
                        {role.type === 2 && (<Button size="small" type="text" icon={<EditOutlined />} onClick={() => {setEditName(role.name); setEditColor(role.color); setEditVisible(true);}}/>)}
                    </Space>
                    <Space size="small">
                        {users.length > 0 && (
                            <Popover title="Пользователи роли"
                                content={
                                    <div style={{ maxHeight: 250, overflowY: "auto", paddingRight: 4 }}>
                                        <Space direction="vertical" style={{ width: "100%" }}>{users.map(u => (<Text key={u.userId}>{u.userName}</Text>))}</Space>
                                    </div>
                                }
                            >
                                <Button size="small">Пользователи</Button>
                            </Popover>
                        )}
                        {role.type === 2 && (<Button size="small" danger type="text" onClick={handleDeleteRole}>Удалить</Button>)}
                    </Space>
                </div>
                {role.type !== 3 && (
                    <>
                        <Text strong>Глобальные права:</Text>
                        <Space wrap>
                            {globalPerms.map(([key]) => (
                                <Tag key={key} closable onClose={e => {e.preventDefault(); handleRemoveGlobalPerm(key);}}>{PERMISSION_LABELS[key]}</Tag>
                            ))}
                            {Object.entries(role.permissions).some(([, v]) => !v) && (
                                <Popover title="Добавить право"trigger="click"
                                    content={
                                        <Space direction="vertical" style={{ maxWidth: 220 }}>
                                            {Object.entries(role.permissions)
                                                .filter(([, v]) => !v)
                                                .map(([key]) => (
                                                    <Button key={key} type="text" size="small" onClick={() => handleAddGlobalPerm(key)}>{PERMISSION_LABELS[key]}</Button>
                                                ))}
                                        </Space>
                                    }
                                >
                                    <Button size="small" icon={<PlusOutlined />}>Добавить</Button>
                                </Popover>
                            )}
                        </Space>
                    </>
                )}
                {channelRights.length > 0 && (
                    <>
                        <Text strong>Права в каналах:</Text>
                        {channelRights.map(([label, arr]) => (
                            <div key={label} style={{ display: "flex", alignItems: "flex-start" }}>
                                <Text style={{ minWidth: 200, display: "inline-block" }}>{label}:</Text>
                                <Space wrap>{arr.map(c => (<Tag key={c.id}>{c.name}</Tag>))}</Space>
                            </div>
                        ))}
                    </>
                )}
            </Space>
            <Modal title="Редактировать роль" open={editVisible} onCancel={() => setEditVisible(false)} okText="Сохранить" cancelText="Отмена"
                okButtonProps={{disabled: editName.length < 1 || editName.length > 100 || !/^#[0-9A-Fa-f]{6}$/.test(editColor)}}
                onOk={() => {
                    console.log("Сохранить роль:", {id: role.id, name: editName, color: editColor});
                    message.success("Роль обновлена");
                    setEditVisible(false);
                }}
            >
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Text strong>Название роли</Text>
                    <Input style={{width: "100%"}} value={editName} onChange={e => setEditName(e.target.value)} maxLength={100}/>
                    <Text strong>Цвет роли</Text>
                    <ColorPicker style={{width: "100%"}} value={editColor} onChange={c => setEditColor(c.toHexString())} showText
                        presets={[
                            {
                                label: "Стандартные",
                                colors: [
                                    "#ffffff",
                                    "#000000",
                                    "#f5222d",
                                    "#fa8c16",
                                    "#fadb14",
                                    "#52c41a",
                                    "#1677ff",
                                    "#722ed1"
                                ]
                            }
                        ]}
                    />
                </Space>
            </Modal>
        </Card>
    );
}

function ServerUserCard({ user, roles, serverData }) 
{
    const dispatch = useDispatch();

    const [iconSrc, setIconSrc] = useState(null);
    const [iconLoading, setIconLoading] = useState(false);

    const [editNameVisible, setEditNameVisible] = useState(false);
    const [editUserName, setEditUserName] = useState(user.userName);

    useEffect(() => {
        if(!user.icon?.fileId) 
        {
            setIconSrc(null);
            setIconLoading(false);
            return;
        }
        setIconLoading(true);
        dispatch(getIconThunkCreator(user.icon.fileId, null))
            .then(data => data && setIconSrc(`data:${data.fileType};base64,${data.base64File}`))
            .finally(() => setIconLoading(false));
    }, [user.icon?.fileId, dispatch]);

    const perms = {};
    user.roles.forEach(ur => {
        const role = serverData.roles.find(r => r.name === ur.roleName);
        if(!role) 
        {
            return;
        }
        Object.entries(role.permissions).forEach(([k, v]) => { perms[k] = perms[k] || v; });
    });

    const channels = { see: new Set(), write: new Set(), writeSub: new Set(), notify: new Set(), join: new Set(), use: new Set() };
    user.roles.forEach(ur => {
        const role = serverData.roles.find(r => r.name === ur.roleName);
        if(!role) 
        {
            return;
        }
        role.channelCanSee.forEach(c => channels.see.add(c.name));
        role.channelCanWrite.forEach(c => channels.write.add(c.name));
        role.channelCanWriteSub.forEach(c => channels.writeSub.add(c.name));
        role.channelNotificated.forEach(c => channels.notify.add(c.name));
        role.channelCanJoin.forEach(c => channels.join.add(c.name));
        role.channelCanUse.forEach(c => channels.use.add(c.name));
    });

    const hasGlobalPerms = Object.values(perms).some(Boolean);
    const hasChannelPerms = channels.see.size > 0 || channels.write.size > 0 || channels.writeSub.size > 0 || channels.notify.size > 0 || channels.join.size > 0 || channels.use.size > 0;

    const handleRemoveRole = roleId => {
        console.log("Удалить роль у пользователя:", { userId: user.userId, roleId });
        message.info("Роль удалена у пользователя (пока локально)");
    };

    const userRoleIds = user.roles.map(r => r.roleId);
    const hasRootRole = user.roles.some(r => r.roleType === 0);
    const availableRoles = hasRootRole ? [] : roles.filter(r => (r.type === 1 || r.type === 2 || r.type === 3) && !userRoleIds.includes(r.id));

    const handleAddRole = role => {
        console.log("Добавить роль пользователю:", { userId: user.userId, roleId: role.id });
        message.success(`Роль "${role.name}" добавлена (пока локально)`);
    };

    const handleDeleteUser = () => {
        if(hasRootRole)
        {
            return;
        }

        Modal.confirm({
            title: `Удалить пользователя "${user.userName}"?`,
            content: "Это действие невозможно отменить.",
            okText: "Удалить",
            okType: "danger",
            cancelText: "Отмена",
            onOk() {
                console.log("Удалить пользователя:", user.userId);
                message.success(`Пользователь "${user.userName}" удалён (пока локально)`);
            }
        });
    };

    return (
        <Card size="small" style={{ marginBottom: 12 }}>
            <Space direction="vertical" style={{ width: "100%" }} size={4}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%" }}>
                    <Space align="center">
                        <Avatar size={40} src={!iconLoading ? iconSrc : null} icon={iconLoading ? <Spin size="small" /> : <UserOutlined />}/>
                        <Text strong>{user.userName} ({user.userName}) {user.userTag}</Text>
                        <Button size="small" type="text" icon={<EditOutlined />} onClick={() => { setEditUserName(user.userName); setEditNameVisible(true); }}/>
                        {!hasRootRole && (<Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={handleDeleteUser}/>)}
                    </Space>
                    {hasChannelPerms && (
                        <Popover title="Права в каналах"
                            content={
                                <Space direction="vertical">
                                    {channels.see.size > 0 && <Text>Может видеть: {[...channels.see].join(", ")}</Text>}
                                    {channels.write.size > 0 && <Text>Может писать: {[...channels.write].join(", ")}</Text>}
                                    {channels.writeSub.size > 0 && <Text>Может создавать подканалы: {[...channels.writeSub].join(", ")}</Text>}
                                    {channels.notify.size > 0 && <Text>Получает обязательные уведомления: {[...channels.notify].join(", ")}</Text>}
                                    {channels.join.size > 0 && <Text>Может присоединяться: {[...channels.join].join(", ")}</Text>}
                                    {channels.use.size > 0 && <Text>Может пользоваться: {[...channels.use].join(", ")}</Text>}
                                </Space>
                            }
                        >
                            <Button size="small">Права в каналах</Button>
                        </Popover>
                    )}
                </div>
                {user.roles.length > 0 && (
                    <>
                        <Text strong>Роли:</Text>
                        <Space wrap>
                            {user.roles.map(r => (
                                <Tag
                                    key={r.roleId}
                                    color={r.colour}
                                    style={{ color: getContrastTextColor(r.colour) }}
                                    closable={r.roleType === 1 || r.roleType === 2}
                                    closeIcon={<CloseOutlined style={{ color: getContrastTextColor(r.colour), opacity: 0.8 }} />}
                                    onClose={e => { e.preventDefault(); handleRemoveRole(r.roleId); }}
                                >
                                    {r.roleName}
                                </Tag>
                            ))}
                            {availableRoles.length > 0 && !hasRootRole && (
                                <Popover title="Добавить роль" trigger="click"
                                    content={
                                        <div style={{ maxWidth: 240, maxHeight: 300, overflowY: "auto" }}>
                                            <Space direction="vertical" style={{ width: "100%" }}>
                                                {availableRoles.map(role => (
                                                    <Button key={role.id} type="text" size="small" onClick={() => handleAddRole(role)} style={{ width: "100%", textAlign: "left" }}>
                                                        <Tag color={role.color} style={{ color: getContrastTextColor(role.color), marginRight: 6 }}>{role.name}</Tag>
                                                    </Button>
                                                ))}
                                            </Space>
                                        </div>
                                    }
                                >
                                    <Button size="small" icon={<PlusOutlined />}>Добавить</Button>
                                </Popover>
                            )}
                        </Space>
                    </>
                )}
                {user.systemRoles.length > 0 && (
                    <>
                        <Text strong>Системные роли:</Text>
                        <Space wrap>{user.systemRoles.map(sr => (<Tag key={sr.name} color={sr.type === 1 ? "gold" : "blue"}>{sr.name}</Tag>))}</Space>
                    </>
                )}
                {hasGlobalPerms && (
                    <>
                        <Text strong>Суммарные права:</Text>
                        <Space wrap>{Object.entries(perms).filter(([, v]) => v).map(([k]) => <Tag key={k}>{PERMISSION_LABELS[k]}</Tag>)}</Space>
                    </>
                )}
            </Space>
            <Modal title="Редактировать имя пользователя" open={editNameVisible}
                onCancel={() => setEditNameVisible(false)} okText="Сохранить" cancelText="Отмена"
                okButtonProps={{ disabled: editUserName.trim().length < 6 || editUserName.trim().length > 50 }}
                onOk={() => {
                    console.log("Изменить имя пользователя:", { userId: user.userId, userName: editUserName.trim() });
                    message.success("Имя обновлено (пока локально)");
                    setEditNameVisible(false);
                }}
            >
                <Input value={editUserName} onChange={e => setEditUserName(e.target.value)} maxLength={50} />
                <Text type={editUserName.trim().length >= 6 ? "secondary" : "danger"}>Имя должно быть от 6 до 50 символов</Text>
            </Modal>
        </Card>
    );
}

const CHANNEL_TYPE_LABELS = {
    textChannels: "Текстовый",
    voiceChannels: "Голосовой",
    notificationChannels: "Уведомления",
    pairVoiceChannels: "Парный голосовой"
};

function ServerChannelCard({ channel, roles, channelKey }) 
{
    const [editVisible, setEditVisible] = useState(false);
    const [editName, setEditName] = useState(channel.channelName);
    const [editMaxCount, setEditMaxCount] = useState(channel.maxCount || 2);

    const rightsMap = {};
    roles.forEach(role => {
        const addRole = (perm, roleChannels) => {
            if(roleChannels?.some(c => c.id === channel.channelId || c.channelId === channel.channelId)) 
            {
                if(!rightsMap[perm]) 
                {
                    rightsMap[perm] = [];
                }
                rightsMap[perm].push(role);
            }
        };
        if(channelKey === "textChannels")
        {
            addRole("Могут видеть", role.channelCanSee);
            addRole("Могут писать", role.channelCanWrite);
            addRole("Могут создавать подканалы", role.channelCanWriteSub);
        } 
        else if(channelKey === "notificationChannels") 
        {
            addRole("Могут видеть", role.channelCanSee);
            addRole("Могут писать", role.channelCanWrite);
            addRole("Получают обязательные уведомления", role.channelNotificated);
        } 
        else if(channelKey === "voiceChannels") 
        {
            addRole("Могут видеть", role.channelCanSee);
            addRole("Могут присоединяться", role.channelCanJoin);
        }
    });

    let extraInfo = null;
    if(channel.messagesNumber !== undefined) 
    {
        extraInfo = `${channel.messagesNumber} сообщений`;
    } 
    else if(channel.maxCount !== undefined) 
    {
        extraInfo = `Макс. пользователей: ${channel.maxCount}`;
    }

    const handleRemoveRoleFromPerm = (perm, role) => {
        if(role.type === 0)
        {
            return;
        }
        console.log(`Удалить роль "${role.name}" из права "${perm}"`);
        message.info(`Роль "${role.name}" удалена из права "${perm}" (пока локально)`);
    };

    const handleAddRoleToPerm = (perm, role) => {
        console.log(`Добавить роль "${role.name}" к праву "${perm}"`);
        message.success(`Роль "${role.name}" добавлена к праву "${perm}" (пока локально)`);
    };

    const getAvailableRolesForPerm = (permRoles) => {
        return roles.filter(r => !permRoles.some(pr => pr.id === r.id));
    };

    const handleDeleteChannel = () => {
        Modal.confirm({
            title: `Удалить канал "${channel.channelName}"?`,
            content: "Это действие невозможно отменить.",
            okText: "Удалить",
            okType: "danger",
            cancelText: "Отмена",
            onOk() {
                console.log("Удалить канал:", channel.channelId);
                message.success(`Канал "${channel.channelName}" удалён (пока локально)`);
            }
        });
    };

    return (
        <Card size="small" style={{ marginBottom: 12 }}>
            <Space direction="vertical" style={{ width: "100%" }} size={4}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%" }}>
                    <Space align="center">
                        <Text strong>{channel.channelName}</Text>
                        <Tag color="blue" style={{ fontWeight: 500 }}>{CHANNEL_TYPE_LABELS[channelKey]}</Tag>
                        <Button size="small" type="text" icon={<EditOutlined />}
                            onClick={() => {
                                setEditName(channel.channelName);
                                if(channel.maxCount !== undefined)
                                {
                                    setEditMaxCount(channel.maxCount);
                                }
                                setEditVisible(true);
                            }}
                        />
                        <Button size="small" type="text" icon={<DeleteOutlined />} danger onClick={handleDeleteChannel}/>
                    </Space>
                    {extraInfo && <Text type="secondary">{extraInfo}</Text>}
                </div>
                {Object.keys(rightsMap).length > 0 && (
                    <>
                        <Text strong>Роли по правам:</Text>
                        {Object.entries(rightsMap).map(([perm, rolesList]) => (
                            <Card key={perm} size="small" style={{ marginBottom: 4 }}>
                                <div style={{ display: "flex", alignItems: "center" }}>
                                    <Text style={{ minWidth: 200, fontWeight: 500 }}>{perm}:</Text>
                                    <Space wrap>
                                        {rolesList.map(r => (
                                            <Tag key={r.id} closable={r.type !== 0} onClose={e => {e.preventDefault(); handleRemoveRoleFromPerm(perm, r);}}
                                                style={{backgroundColor: r.color, color: getContrastTextColor(r.color), fontWeight: 500, opacity: r.type === 0 ? 0.6 : 1}}
                                            >
                                                {r.name}
                                            </Tag>
                                        ))}
                                        {getAvailableRolesForPerm(rolesList).length > 0 && (
                                            <Popover title={`Добавить роль к "${perm}"`} trigger="click"
                                                content={
                                                    <div style={{ maxHeight: 200, overflowY: "auto" }}>
                                                        <Space direction="vertical" style={{ width: "100%" }}>
                                                            {getAvailableRolesForPerm(rolesList).map(r => (
                                                                <Button key={r.id} type="text" size="small" style={{ width: "100%", textAlign: "left" }} onClick={() => handleAddRoleToPerm(perm, r)}>
                                                                    <Tag color={r.color} style={{ color: getContrastTextColor(r.color), marginRight: 6 }}>{r.name}</Tag>
                                                                </Button>
                                                            ))}
                                                        </Space>
                                                    </div>
                                                }
                                            >
                                                <Button size="small" icon={<PlusOutlined />} />
                                            </Popover>
                                        )}
                                    </Space>
                                </div>
                            </Card>
                        ))}
                    </>
                )}
            </Space>
            <Modal title="Редактировать канал" open={editVisible} onCancel={() => setEditVisible(false)} okText="Сохранить" cancelText="Отмена"
                okButtonProps={{disabled: editName.trim().length < 1 || editName.trim().length > 100 || (channel.maxCount !== undefined && (editMaxCount < 2 || editMaxCount > 999))}}
                onOk={() => {
                    console.log("Сохранить канал:", { channelId: channel.channelId, name: editName.trim(), maxCount: editMaxCount });
                    message.success("Канал обновлен (пока локально)");
                    setEditVisible(false);
                }}
            >
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Text strong>Название канала</Text>
                    <Input value={editName} onChange={e => setEditName(e.target.value)} maxLength={100} />
                    {channel.maxCount !== undefined && (
                        <>
                            <Text strong>Максимальное количество пользователей</Text>
                            <Input type="number" value={editMaxCount} onChange={e => setEditMaxCount(Number(e.target.value))} min={2} max={999}/>
                        </>
                    )}
                </Space>
            </Modal>
        </Card>
    );
}

function ServerPresetCard({ preset }) 
{
    const handleDeletePreset = () => {
        Modal.confirm({title: `Удалить пресет "${preset.serverRoleName} → ${preset.systemRoleName}"?`, content: "Это действие невозможно отменить.", okText: "Удалить", okType: "danger", cancelText: "Отмена",
            onOk() {
                console.log("Удалить пресет:", preset);
                message.success(`Пресет "${preset.serverRoleName} → ${preset.systemRoleName}" удалён (пока локально)`);
            }
        });
    };

    return (
        <Card size="small" style={{ marginBottom: 12 }}>
            <Space align="center" style={{ justifyContent: "space-between", width: "100%" }}>
                <Text>
                    <Tag color="purple">{preset.serverRoleName}</Tag> →{" "}
                    <Tag color={preset.systemRoleType === 1 ? "gold" : "blue"}>{preset.systemRoleName}</Tag>
                </Text>
                <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={handleDeletePreset}/>
            </Space>
        </Card>
    );
}
