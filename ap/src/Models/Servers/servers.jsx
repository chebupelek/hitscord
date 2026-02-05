import { Button, Col, Row, Card, Select, Input, Space, Pagination, Form, Skeleton } from "antd";
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { getServersListThunkCreator  } from "../../Reducers/ServersReducer";
import { useNavigate, useSearchParams } from "react-router-dom";
import ServerCard from "./serverCard";

function ServersFilter({ name, selectedSize, setName, setSelectedSize, handleSearch }) 
{
    return (
        <Card style={{ width: '100%', boxSizing: 'border-box', backgroundColor: '#f6f6fb' }}>
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <h2 style={{ margin: 0, fontSize: '1.5em' }}>Фильтры и сортировка</h2>
                <Row gutter={16}>
                    <Col span={12}>
                        <span>Название</span>
                        <Input style={{ width: '100%' }} value={name} onChange={e => setName(e.target.value)} />
                    </Col>
                    <Col span={12}>
                    </Col>
                </Row>
                <Row gutter={16} align="middle" style={{ marginTop: '10px' }}>
                    <Col span={12}>
                    </Col>
                    <Col span={12}>
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
        <Row justify="center" style={{ marginTop: '2%', marginBottom: '2%' }}>
            <Pagination current={parseInt(current)} pageSize={1} total={total} onChange={onChange} showSizeChanger={false}/>
        </Row>
    );
}

function Servers() 
{
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const servers = useSelector(state => state.servers.servers) || [];
    const pagination = useSelector(state => state.servers.pagination);

    const loadingServers = useSelector(state => state.servers.loadingServers);

    const [searchParams, setSearchParams] = useSearchParams();
    const [name, setName] = useState("");
    const [selectedSize, setSelectedSize] = useState("6");
    const [selectedPage, setSelectedPage] = useState("1");

    const handleSearch = () => {
        const queryParams = [
            name ? `name=${encodeURIComponent(name)}` : '',
            `page=1`,
            `num=${selectedSize}`
        ].filter(Boolean).join('&');
        setSelectedPage("1");
        setSearchParams(queryParams);
    };

    const handleChangePage = (page) => {
        setSelectedPage(page.toString());
        searchParams.set('page', page.toString());
        setSearchParams(searchParams);
    };

    useEffect(() => {
        const nameParam = searchParams.get('name') || "";
        const sizeParam = searchParams.get('num') || 6;
        const pageParam = searchParams.get('page') || 1;

        setName(nameParam);
        setSelectedSize(sizeParam);
        setSelectedPage(pageParam);

        const queryParams = [
            nameParam ? `name=${encodeURIComponent(nameParam)}` : '',
            `page=${pageParam}`,
            `num=${sizeParam}`
        ].filter(Boolean).join('&');

        dispatch(getServersListThunkCreator(queryParams, navigate));
    }, [searchParams, dispatch]);

    return (
        <div style={{ width: '75%', marginBottom: '2%' }}>
            <Row align="middle">
                <h1>Сервера</h1>
            </Row>
            <ServersFilter name={name} selectedSize={selectedSize} setName={setName} setSelectedSize={setSelectedSize} handleSearch={handleSearch}/>
            {loadingServers ? (
                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                    {[...Array(parseInt(selectedSize))].map((_, i) => (<Skeleton key={i} active paragraph={{ rows: 4 }} />))}
                </Space>
            ) : (
                <>
                    <Row gutter={16} style={{ marginTop: '2%' }}>
                        {servers.map(server => (
                            <Col key={server.id} span={24}>
                                <ServerCard id={server.id} serverName={server.serverName} serverType={server.serverType} usersNumber={server.usersNumber} icon={server.icon}/>
                            </Col>
                        ))}
                    </Row>
                    <UsersPagination current={selectedPage} total={pagination.PageCount} onChange={handleChangePage}/>
                </>
            )}
        </div>
    );
}

export default Servers;
