import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { Spin, Card, Typography, Avatar, Tag, Space, Divider } from "antd";
import { DatabaseOutlined, TeamOutlined } from "@ant-design/icons";
import { getServerDataThunkCreator, clearServerDataActionCreator } from "../../Reducers/ServersReducer";
import { getIconThunkCreator } from "../../Reducers/UsersListReducer";

const { Text, Title } = Typography;

function ServerInfoPage() {
    const { id } = useParams(); // Получаем id сервера из URL
    const dispatch = useDispatch();

    const serverData = useSelector(state => state.servers.serverData);
    const loading = useSelector(state => state.servers.loadingServerData);

    const [iconSrc, setIconSrc] = useState(null);
    const [iconLoading, setIconLoading] = useState(false);

    // Загружаем данные сервера
    useEffect(() => {
    // Функция для сериализации query-параметров
    const toQueryString = (params) =>
        Object.entries(params)
            .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
            .join('&');

    // Загружаем данные сервера
    if (id) {
        const queryString = toQueryString({ ServerId: id });
        dispatch(getServerDataThunkCreator(queryString, null));
    }

    // Очистка при размонтировании страницы
    return () => {
        dispatch(clearServerDataActionCreator());
    };
}, [id, dispatch]);

    // Загружаем иконку сервера через thunk
    useEffect(() => {
        if (!serverData?.icon?.fileId) {
            setIconSrc(null);
            setIconLoading(false);
            return;
        }

        setIconLoading(true);
        dispatch(getIconThunkCreator(serverData.icon.fileId, null))
            .then(data => {
                if (data) {
                    setIconSrc(`data:${data.fileType};base64,${data.base64File}`);
                }
            })
            .finally(() => setIconLoading(false));
    }, [serverData?.icon?.fileId, dispatch]);

    if (loading || !serverData) {
        return (
            <div style={{ textAlign: "center", marginTop: "20%" }}>
                <Spin size="large" />
            </div>
        );
    }

    const serverTypeLabel = serverData.serverType === 0 ? "Студенческий" : "Учебный";
    const serverTypeColor = serverData.serverType === 0 ? "blue" : "green";

    return (
        <div style={{ maxWidth: 900, margin: "20px auto" }}>
            <Card>
                <Space align="center" style={{ width: "100%", justifyContent: "space-between" }}>
                    <Space align="center">
                        <Avatar
                            size={60}
                            src={!iconLoading ? iconSrc : null}
                            icon={iconLoading ? <Spin size="small" /> : <DatabaseOutlined />}
                        />
                        <div>
                            <Title level={3} style={{ margin: 0 }}>{serverData.serverName}</Title>
                            <Space>
                                <Tag color={serverTypeColor}>{serverTypeLabel}</Tag>
                                <Tag icon={<TeamOutlined />}>{serverData.users.length}</Tag>
                            </Space>
                        </div>
                    </Space>
                </Space>

                <Divider />

                {/* Роли сервера */}
                <div>
                    <Title level={5}>Роли:</Title>
                    <Space wrap>
                        {serverData.roles.map(role => (
                            <Tag key={`${role.id}-${role.tag}`} color={role.color}>
                                {role.name} ({role.tag})
                            </Tag>
                        ))}
                    </Space>
                </div>

                <Divider />

                {/* Пользователи сервера */}
                <div>
                    <Title level={5}>Пользователи:</Title>
                    <Space wrap>
                        {serverData.users.map(user => (
                            <Tag key={`${user.userId}-${user.userTag}`}>
                                {user.userName}#{user.userTag}
                            </Tag>
                        ))}
                    </Space>
                </div>

                <Divider />

                {/* Каналы */}
                <div>
                    <Title level={5}>Каналы:</Title>
                    <Space direction="vertical">
                        <div>
                            <Text strong>Текстовые каналы: </Text>
                            {serverData.channels.textChannels.map(c => (
                                <Tag key={c.channelId}>{c.channelName} ({c.messagesNumber})</Tag>
                            ))}
                        </div>
                        <div>
                            <Text strong>Голосовые каналы: </Text>
                            {serverData.channels.voiceChannels.map(c => (
                                <Tag key={c.channelId}>{c.channelName} ({c.maxCount})</Tag>
                            ))}
                        </div>
                    </Space>
                </div>
            </Card>
        </div>
    );
}

export default ServerInfoPage;
