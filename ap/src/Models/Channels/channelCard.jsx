import { Card, Typography, Button, Spin } from "antd";
import { useDispatch, useSelector } from "react-redux";
import { rewiveChannelThunkCreator } from "../../Reducers/ChannelsListReducer";

function formatDate(dateString) 
{
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

function ChannelCard({ id, name, serverId, serverName, DelteTime }) 
{
    const dispatch = useDispatch();
    const loadingRewiving = useSelector(state => state.channels.loadingRewiving);

    const handleRestore = () => {
        const payload = { id };
        dispatch(rewiveChannelThunkCreator(payload));
    };

    return (
        <Card style={{ width: '100%', boxSizing: 'border-box', backgroundColor: '#f6f6fb', marginTop: '1%' }}>
            <Spin spinning={loadingRewiving}>
                <Typography.Title level={4} style={{ margin: 0 }}>{name}</Typography.Title>

                <div>Id - <strong>{id}</strong></div>
                <div>Сервер - <strong>{serverName}</strong></div>
                <div>Id сервера - <strong>{serverId}</strong></div>
                <div>Дата удаления - <strong>{formatDate(DelteTime)}</strong></div>

                <Button
                    type="primary"
                    style={{ backgroundColor: "green", marginTop: "10px" }}
                    onClick={handleRestore}
                >
                    Восстановить канал
                </Button>
            </Spin>
        </Card>
    );
}


export default ChannelCard;
