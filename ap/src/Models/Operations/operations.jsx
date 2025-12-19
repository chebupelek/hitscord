import { Button, Col, Row, Card, Select, Pagination, Spin } from "antd";
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { getOperationsThunkCreator } from "../../Reducers/OperationsReducer";
import { useNavigate, useSearchParams } from "react-router-dom";
import OperationCard from "./operationCard";

function Operations() 
{
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const operations = useSelector(state => state.operations.operations)  || [];
    const pagination = useSelector(state => state.operations.pagination);
    const loadingOperations = useSelector(state => state.operations.loadingOperations);

    const [searchParams, setSearchParams] = useSearchParams();

    const [selectedSize, setSelectedSize] = useState("6");
    const [selectedPage, setSelectedPage] = useState("1");

    useEffect(() => {
        const sizeParam = searchParams.get('num') || 6;
        const pageParam = searchParams.get('page') || 1;

        setSelectedSize(sizeParam);
        setSelectedPage(pageParam);

        const queryParams = [
            `page=${pageParam}`,
            `num=${sizeParam}`
        ].filter(Boolean).join('&');

        dispatch(getOperationsThunkCreator(queryParams, navigate));
    }, [searchParams, dispatch]);

    const handleSearch = () => {
        const queryParams = [
            `page=${1}`,
            `num=${selectedSize}`
        ].filter(Boolean).join('&');
        setSelectedPage(1);
        setSearchParams(queryParams);
    };

    const handleChangePage = (page) => {
        setSelectedPage(page.toString());
        searchParams.set('page', page.toString());
        setSearchParams(searchParams);
    };

    return (
        <div style={{ width: '75%' }}>
            <Row align="middle">
                <h1>История операций администраторов</h1>
            </Row>
            <Card style={{ width: '100%', boxSizing: 'border-box', backgroundColor: '#f6f6fb' }}>
                <div style={{ width: '100%' }}>
                    <Row gutter={16} align="middle">
                        <Col span={8}>
                            <span>Число операций на странице</span>
                            <Select style={{ width: '100%' }} value={selectedSize} onChange={value => setSelectedSize(value)}>
                                <Select.Option value="6">6</Select.Option>
                                <Select.Option value="7">7</Select.Option>
                                <Select.Option value="8">8</Select.Option>
                                <Select.Option value="9">9</Select.Option>
                                <Select.Option value="10">10</Select.Option>
                                <Select.Option value="20">20</Select.Option>
                                <Select.Option value="30">30</Select.Option>
                                <Select.Option value="40">40</Select.Option>
                                <Select.Option value="100">100</Select.Option>
                            </Select>
                        </Col>
                        <Col span={8}></Col>
                        <Col span={8}>
                            <Button type="primary" block style={{ backgroundColor: '#317dba', marginLeft: 'auto' }} onClick={handleSearch}>
                                Поиск
                            </Button>
                        </Col>
                    </Row>
                </div>
            </Card>
            <Spin spinning={loadingOperations}>
                {operations ? 
                    <div>
                        <Row gutter={16} style={{ marginTop: '2%' }}>
                            {operations.map(operation => (
                                <Col key={operation.id} span={24}>
                                    <OperationCard
                                        id={operation.id}
                                        opaerationTime={operation.opaerationTime}
                                        adminName={operation.adminName}
                                        operation={operation.operation}
                                        operationData={operation.operationData}
                                    />
                                </Col>
                            ))}
                        </Row>
                        {pagination.PageCount > 1 && (
                            <Row justify="center" style={{ marginTop: '2%', marginBottom: '2%' }}>
                                <Pagination current={parseInt(selectedPage)} pageSize={1} total={pagination.PageCount} onChange={handleChangePage} showSizeChanger={false}/>
                            </Row>
                        )}
                    </div> : 
                <></>}
            </Spin>
        </div>
    );
}

export default Operations;