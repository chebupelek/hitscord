import { Button, Col, Row, Card, Select, Input, Space, Pagination, Modal, Form, Skeleton, Spin } from "antd";
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { getUsersListThunkCreator, getRolesShortListThunkCreator, addRoleThunkCreator } from "../../Reducers/UsersListReducer";
import { useNavigate, useSearchParams } from "react-router-dom";
import UserCard from "./userCard";

function UsersFilter({ name, mail, selectedSort, rolesId, selectedSize, setName, setMail, setSelectedSort, setRolesId, setSelectedSize, handleSearch, rolesShort, fetchRoles, handleSelectAll, isAllSelected, loadingRolesShort }) 
{
    return (
        <Card style={{ width: '100%', boxSizing: 'border-box', backgroundColor: '#f6f6fb' }}>
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <h2 style={{ margin: 0, fontSize: '1.5em' }}>Фильтры и сортировка</h2>
                <Row gutter={16}>
                    <Col span={12}>
                        <span>Имя</span>
                        <Input style={{ width: '100%' }} value={name} onChange={e => setName(e.target.value)} />
                    </Col>
                    <Col span={12}>
                        <span>Почта</span>
                        <Input style={{ width: '100%' }} value={mail} onChange={e => setMail(e.target.value)} />
                    </Col>
                </Row>
                <Row gutter={16} align="middle" style={{ marginTop: '10px' }}>
                    <Col span={12}>
                        <span>Сортировка пользователей</span>
                        <Select style={{ width: '100%' }} value={selectedSort} onChange={setSelectedSort}>
                            <Select.Option value=""></Select.Option>
                            <Select.Option value="NameAsc">По имени пользователя (от А-Я)</Select.Option>
                            <Select.Option value="NameDesc">По имени пользователя (от Я-А)</Select.Option>
                            <Select.Option value="MailAsc">По почте пользователя (от А-Я)</Select.Option>
                            <Select.Option value="MailDesc">По почте пользователя (от Я-А)</Select.Option>
                            <Select.Option value="AccountNumberAsc">По номеру пользователя (сначала малые)</Select.Option>
                            <Select.Option value="AccountNumberDesc">По номеру пользователя (сначала большие)</Select.Option>
                        </Select>
                    </Col>
                    <Col span={12}>
                        <span>Фильтр по ролям</span>
                        <Spin spinning={loadingRolesShort}>
                            <Select mode="multiple" allowClear placeholder="Выберите роли"
                                value={rolesId} style={{ width: '100%' }} showSearch
                                filterOption={false} onFocus={fetchRoles}
                                onSearch={value => fetchRoles(`name=${value}`)} onChange={setRolesId}
                                onInputKeyDown={(e) => { if (e.target.value === "") fetchRoles(); }}
                            >
                                {rolesShort.map(role => <Select.Option key={role.id} value={role.id}>{role.name}</Select.Option>)}
                            </Select>
                        </Spin>
                    </Col>
                </Row>
                <Form layout="vertical" style={{ marginTop: '10px' }}>
                    <Row gutter={16}>
                        <Col span={8}>
                            <Form.Item label="Число пользователей на странице" labelCol={{ span: 24 }} style={{ marginBottom: 0 }}>
                                <Select value={selectedSize} onChange={setSelectedSize}>
                                    {["6","7","8","9","10","20","30","40","100"].map(num => (<Select.Option key={num} value={num}>{num}</Select.Option>))}
                                </Select>
                            </Form.Item>
                        </Col>
                        <Col span={8}>
                            <Form.Item label="&nbsp;" style={{ marginBottom: 0 }}>
                                <Button block onClick={handleSelectAll}>{isAllSelected ? "Снять выделение" : "Выделить всех"}</Button>
                            </Form.Item>
                        </Col>
                        <Col span={8}>
                            <Form.Item label="&nbsp;" style={{ marginBottom: 0 }}>
                                <Button type="primary" block onClick={handleSearch}>Поиск</Button>
                            </Form.Item>
                        </Col>
                    </Row>
                </Form>
            </Space>
        </Card>
    );
}

function UsersPagination({ current, total, onChange }) 
{
    if (total <= 1) 
    {
        return null;
    }
    return (
        <Row justify="center" style={{ marginTop: '2%' }}>
            <Pagination current={parseInt(current)} pageSize={1} total={total} onChange={onChange} showSizeChanger={false}/>
        </Row>
    );
}

function AddRoleModal({ visible, handleCancel, rolesShort, selectedRoleId, setSelectedRoleId, isSubmitDisabled, handleAddRole, fetchRoles, loadingAddRole }) 
{
    return (
        <Modal open={visible} onCancel={handleCancel} closeIcon={false}
            footer={[
                <Button key="submit" type="primary" block disabled={isSubmitDisabled || loadingAddRole} style={{ backgroundColor: "#317cb9" }} onClick={handleAddRole}>
                    {loadingAddRole ? <Spin size="small" /> : "Добавить роль"}
                </Button>
            ]}
        >
            <Space direction="vertical" size="middle" style={{ width: "100%" }}>
                <h1>Добавление роли</h1>
                <Select key={selectedRoleId} value={selectedRoleId} showSearch
                    placeholder="Выберите роль" filterOption={false} onFocus={fetchRoles}
                    onSearch={value => fetchRoles(`name=${value}`)} onChange={setSelectedRoleId}
                    onInputKeyDown={(e) => { if (e.target.value === "") fetchRoles(); }}
                    style={{ width: "100%" }}
                >
                    {rolesShort.map(role => <Select.Option key={role.id} value={role.id}>{role.name}</Select.Option>)}
                </Select>
            </Space>
        </Modal>
    );
}

function Users() 
{
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const users = useSelector(state => state.users.users) || [];
    const pagination = useSelector(state => state.users.pagination);
    const rolesShort = useSelector(state => state.users.roles) || [];

    const loadingUsers = useSelector(state => state.users.loadingUsers);
    const loadingRolesShort = useSelector(state => state.users.loadingRolesShort);
    const loadingAddRole = useSelector(state => state.users.loadingAddRole);
    const loadingRemoveRole = useSelector(state => state.users.loadingRemoveRole);

    const [searchParams, setSearchParams] = useSearchParams();
    const [name, setName] = useState("");
    const [mail, setMail] = useState("");
    const [selectedSort, setSelectedSort] = useState("");
    const [selectedSize, setSelectedSize] = useState("6");
    const [selectedPage, setSelectedPage] = useState("1");
    const [rolesId, setRolesId] = useState([]);
    const [selectedUsers, setSelectedUsers] = useState([]);
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [selectedRoleId, setSelectedRoleId] = useState(null);

    const isAddDisabled = selectedUsers.length === 0;
    const isSubmitDisabled = selectedUsers.length === 0 || !selectedRoleId;

    const fetchRoles = (query = "name=") => dispatch(getRolesShortListThunkCreator(query, navigate));

    const handleSearch = () => {
        const queryParams = [
            name ? `name=${encodeURIComponent(name)}` : '',
            mail ? `mail=${encodeURIComponent(mail)}` : '',
            ...rolesId.map(roleId => `rolesIds=${roleId}`),
            selectedSort ? `sort=${selectedSort}` : '',
            `page=1`,
            `num=${selectedSize}`
        ].filter(Boolean).join('&');
        setSelectedPage("1");
        setSearchParams(queryParams);
    };

    const reloadUsers = () => {
        const queryParams = [
            name ? `name=${encodeURIComponent(name)}` : '',
            mail ? `mail=${encodeURIComponent(mail)}` : '',
            ...rolesId.map(r => `rolesIds=${r}`),
            selectedSort ? `sort=${selectedSort}` : '',
            `page=${selectedPage}`,
            `num=${selectedSize}`
        ].filter(Boolean).join('&');
        dispatch(getUsersListThunkCreator(queryParams, navigate));
    };

    const handleChangePage = (page) => {
        setSelectedPage(page.toString());
        searchParams.set('page', page.toString());
        setSearchParams(searchParams);
    };

    const handleSelectUser = (id, checked) => {
        setSelectedUsers(prev => checked ? [...prev, id] : prev.filter(x => x !== id));
    };

    const allUserIds = users.map(u => u.id);
    const isAllSelected = users.length > 0 && allUserIds.every(id => selectedUsers.includes(id));
    const handleSelectAll = () => setSelectedUsers(isAllSelected ? [] : allUserIds);

    const showModal = () => setIsModalVisible(true);
    const handleCancel = () => setIsModalVisible(false);
    const resetState = () => { setSelectedUsers([]); setSelectedRoleId(null); };
    const handleAddRole = () => {
        const payload = { roleId: selectedRoleId, usersIds: selectedUsers };
        dispatch(addRoleThunkCreator(payload, navigate, resetState, handleCancel, reloadUsers));
    };

    useEffect(() => {
        const nameParam = searchParams.get('name') || "";
        const mailParam = searchParams.get('mail') || "";
        const sortingParam = searchParams.get('sort') || "";
        const sizeParam = searchParams.get('num') || 6;
        const pageParam = searchParams.get('page') || 1;
        const rolesParam = searchParams.getAll('rolesIds') || [];

        setName(nameParam);
        setMail(mailParam);
        setSelectedSort(sortingParam);
        setSelectedSize(sizeParam);
        setSelectedPage(pageParam);
        setRolesId(rolesParam);

        const queryParams = [
            nameParam ? `name=${encodeURIComponent(nameParam)}` : '',
            mailParam ? `mail=${encodeURIComponent(mailParam)}` : '',
            ...rolesParam.map(r => `rolesIds=${r}`),
            sortingParam ? `sort=${sortingParam}` : '',
            `page=${pageParam}`,
            `num=${sizeParam}`
        ].filter(Boolean).join('&');

        dispatch(getUsersListThunkCreator(queryParams, navigate));
    }, [searchParams, dispatch]);

    return (
        <div style={{ width: '75%', marginBottom: '2%' }}>
            <Row align="middle">
                <h1>Пользователи</h1>
                <Button type="primary" style={{ backgroundColor: '#317dba', marginLeft: 'auto' }} disabled={isAddDisabled} onClick={showModal}>Добавить роль</Button>
            </Row>
            <UsersFilter
                name={name} mail={mail} selectedSort={selectedSort} rolesId={rolesId} selectedSize={selectedSize}
                setName={setName} setMail={setMail} setSelectedSort={setSelectedSort} setRolesId={setRolesId} setSelectedSize={setSelectedSize}
                handleSearch={handleSearch} rolesShort={rolesShort} fetchRoles={fetchRoles}
                handleSelectAll={handleSelectAll} isAllSelected={isAllSelected} loadingRolesShort={loadingRolesShort}
            />
            {loadingUsers ? (
                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                    {[...Array(parseInt(selectedSize))].map((_, i) => (
                        <Skeleton key={i} active paragraph={{ rows: 4 }} />
                    ))}
                </Space>
            ) : (
                <>
                    <Row gutter={16} style={{ marginTop: '2%' }}>
                        {users.map(user => (
                            <Col key={user.id} xs={24} xl={12}>
                                <UserCard
                                    id={user.id}
                                    name={user.accountName}
                                    mail={user.mail}
                                    accountTag={user.accountTag}
                                    accountCreateDate={user.accountCreateDate}
                                    systemRoles={user.systemRoles}
                                    checked={selectedUsers.includes(user.id)}
                                    onCheck={handleSelectUser}
                                    reloadUsers={reloadUsers}
                                    loadingRemoveRole={loadingRemoveRole}
                                />
                            </Col>
                        ))}
                    </Row>
                    <UsersPagination current={selectedPage} total={pagination.PageCount} onChange={handleChangePage}/>
                </>
            )}
            <AddRoleModal
                visible={isModalVisible}
                handleCancel={handleCancel}
                rolesShort={rolesShort}
                selectedRoleId={selectedRoleId}
                setSelectedRoleId={setSelectedRoleId}
                isSubmitDisabled={isSubmitDisabled}
                handleAddRole={handleAddRole}
                fetchRoles={fetchRoles}
                loadingAddRole={loadingAddRole}
            />
        </div>
    );
}

export default Users;
