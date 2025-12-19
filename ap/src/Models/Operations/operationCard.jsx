import { Card, Row, Col, Typography } from "antd";

const { Text } = Typography;

function formatDate(dateString) {
    const date = new Date(dateString);

    return date.toLocaleString('ru-RU', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
}

function OperationCard({ opaerationTime, adminName, operation, operationData }) {
    return (
        <Card style={{ width: '100%', boxSizing: 'border-box', backgroundColor: '#f6f6fb', marginTop: '1%' }}>
            <Row align="middle" gutter={16}>
                <Col span={3} style={{ textAlign: 'center' }}>
                    <Text>{formatDate(opaerationTime)}</Text>
                </Col>
                <Col span={3} style={{textAlign: 'center', whiteSpace: 'normal', wordBreak: 'break-word'}}>
                    <Text>{adminName}</Text>
                </Col>
                <Col span={5} style={{ textAlign: 'center' }}>
                    <Text>{operation}</Text>
                </Col>
                <Col span={13} style={{textAlign: 'center', whiteSpace: 'normal', wordBreak: 'break-word'}}>
                    <Text>
                        {operationData}
                    </Text>
                </Col>
            </Row>
        </Card>
    );
}

export default OperationCard;
