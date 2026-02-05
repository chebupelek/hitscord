import { Card, Typography, Avatar, Spin, Tag, Space, Button } from "antd";
import { useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import { useNavigate } from "react-router-dom";
import { DatabaseOutlined, TeamOutlined } from "@ant-design/icons";
import { getIconThunkCreator } from "../../Reducers/UsersListReducer";
import { clearServerDataActionCreator } from "../../Reducers/ServersReducer";

const { Text } = Typography;

function ServerCard({ id, serverName, serverType, usersNumber, icon }) 
{
    const dispatch = useDispatch();
    const navigate = useNavigate();

    const [iconSrc, setIconSrc] = useState(null);
    const [iconLoading, setIconLoading] = useState(false);

    useEffect(() => {
        if (!icon?.fileId) 
        {
            setIconSrc(null);
            return;
        }
        setIconLoading(true);
        dispatch(getIconThunkCreator(icon.fileId, navigate))
            .then(data => {
                if (data) 
                {
                    setIconSrc(`data:${data.fileType};base64,${data.base64File}`);
                }
            })
            .finally(() => setIconLoading(false));
    }, [icon?.fileId, dispatch, navigate]);

    const serverTypeLabel = serverType === 0 ? "Студенческий" : "Учебный";
    const serverTypeColor = serverType === 0 ? "blue" : "green";

    const handleNavigate = () => {
        dispatch(clearServerDataActionCreator());
        navigate(`/server/${id}`);
    };

    return (
        <Card hoverable style={{ width: "100%", padding: "6px 12px", marginBottom: 6 }} bodyStyle={{ padding: 0 }} onClick={handleNavigate}>
            <Space align="center" style={{ width: "100%", whiteSpace: "nowrap", fontSize: '1.2em', justifyContent: 'space-between' }}>
                <Space align="center" size="middle">
                    <Avatar size={42} src={!iconLoading ? iconSrc : null} icon={iconLoading ? <Spin size="small" /> : <DatabaseOutlined style={{ fontSize: '1.2em' }} />}/>
                    <Text strong ellipsis style={{ maxWidth: 300, fontSize: '1.2em' }}>{serverName}</Text>
                    <Text ellipsis style={{ maxWidth: 320, fontSize: '0.9em' }}>{id}</Text>
                </Space>
                <Space align="center" size="small">
                    <Tag color={serverTypeColor} style={{ fontSize: '1em' }}>{serverTypeLabel}</Tag>
                    <Tag icon={<TeamOutlined style={{ fontSize: '1em' }} />} style={{ fontSize: '1em' }}>{usersNumber}</Tag>
                </Space>
            </Space>
        </Card>
    );
}

export default ServerCard;
