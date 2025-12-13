import { Card, Typography, Space, Button, Input, Modal, Spin } from "antd";
import { useDispatch, useSelector } from "react-redux";
import { createRoleThunkCreator, deleteRoleThunkCreator, updateRoleThunkCreator } from "../../Reducers/RolesReducer";
import { useState } from 'react';
import { motion, AnimatePresence } from "framer-motion";

function RoleCard({ id, name, type, childRoles = [], level = 0, navigate, disableDelete = false }) 
{
    const marginPercent = level > 5 ? 0 : level * 2;
    const cardWidthPercent = 100 - marginPercent;

    const loadingCreate = useSelector(state => state.roles.loadingCreate);
    const loadingUpdate = useSelector(state => state.roles.loadingUpdate);
    const loadingDelete = useSelector(state => state.roles.loadingDelete);
    
    const dispatch = useDispatch();

    const [isEditing, setIsEditing] = useState(false);
    const [roleName, setRoleName] = useState(name);
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [newRoleName, setNewRoleName] = useState("");
    const [isCollapsed, setIsCollapsed] = useState(false);

    const typeText = type === 1 ? "Учитель" : "Студент";

    const handleDelete = () => {
        const payload = { id };
        dispatch(deleteRoleThunkCreator(payload, navigate));
    };

    const handleEdit = () => setIsEditing(true);
    const handleCancel = () => { setRoleName(name); setIsEditing(false); };
    const handleSave = () => { dispatch(updateRoleThunkCreator({ id, name: roleName }, navigate)); setIsEditing(false); };
    const showModal = () => { setNewRoleName(""); setIsModalVisible(true); };
    const handleModalCancel = () => setIsModalVisible(false);
    const handleCreateRole = () => { dispatch(createRoleThunkCreator({ id, name: newRoleName }, navigate)); setIsModalVisible(false); };
    const toggleCollapse = () => setIsCollapsed(!isCollapsed);

    return (
        <Card style={{width: `${cardWidthPercent}%`, marginLeft: `${marginPercent}%`, marginTop: '1%', boxSizing: 'border-box', backgroundColor: '#f6f6fb', border: '1px solid #000000ff'}}>
            <Typography.Title level={4} style={{ margin: 0 }}>
                {isEditing ? (
                    <Input value={roleName} onChange={e => setRoleName(e.target.value)} style={{ width: 200 }} disabled={loadingUpdate}/>
                ) : (
                    name
                )}
            </Typography.Title>
            <div>Id - <strong>{id}</strong></div>
            <div>Тип - <strong>{typeText}</strong></div>
            <Space style={{ marginTop: 10, marginBottom: 10 }}>
                {!isEditing ? (
                    <>
                        <Button type="primary" onClick={handleEdit} disabled={loadingUpdate || loadingDelete}>Изменить имя</Button>
                        {(!disableDelete && childRoles.length === 0) && <Button danger onClick={handleDelete} loading={loadingDelete}>Удалить</Button>}
                        <Button type="dashed" onClick={showModal} disabled={loadingCreate} loading={loadingCreate}>Добавить роль</Button>
                    </>
                ) : (
                    <>
                        <Button type="primary" disabled={roleName === name || loadingUpdate} onClick={handleSave} loading={loadingUpdate}>Сохранить</Button>
                        <Button onClick={handleCancel} disabled={loadingUpdate}>Отменить</Button>
                    </>
                )}
            </Space>
            {childRoles.length > 0 && (
                <Button onClick={toggleCollapse} style={{ marginBottom: 10 }}>
                    {isCollapsed ? "Показать наследников" : "Свернуть наследников"}
                </Button>
            )}
            <AnimatePresence>
                {!isCollapsed && childRoles.length > 0 && (
                    <motion.div style={{ marginTop: '10px' }} initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: "auto" }} exit={{ opacity: 0, height: 0 }} transition={{ duration: 0.3 }}>
                        <Space direction="vertical" style={{ width: '100%' }}>
                            {childRoles.map(child => (
                                <RoleCard 
                                    key={child.id} 
                                    id={child.id} 
                                    name={child.name} 
                                    type={child.type} 
                                    childRoles={child.childRoles} 
                                    level={level + 1} 
                                    navigate={navigate}
                                    disableDelete={false}
                                />
                            ))}
                        </Space>
                    </motion.div>
                )}
            </AnimatePresence>
            <Modal title="Создание роли" open={isModalVisible} onCancel={handleModalCancel} footer={[<Button key="create" type="primary" disabled={!newRoleName} onClick={handleCreateRole} loading={loadingCreate}>Создать</Button>]}>
                <Input placeholder="Введите имя роли" value={newRoleName} onChange={e => setNewRoleName(e.target.value)} maxLength={100}/>
            </Modal>
        </Card>
    );
}

export default RoleCard;

