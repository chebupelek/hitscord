import { Col, Row, Spin } from "antd";
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { getRolesFullThunkCreator } from "../../Reducers/RolesReducer";
import { useNavigate } from "react-router-dom";
import RoleCard from "./rolesCard";

function Roles() 
{
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const roles = useSelector(state => state.roles.roles) || [];
    const loadingRoles = useSelector(state => state.roles.loadingRoles);

    useEffect(() => {
        dispatch(getRolesFullThunkCreator(navigate));
    }, [dispatch]);

    return (
        <div style={{ width: '75%', paddingBottom: '50px' }}>
            <Row align="middle">
                <h1>Системные роли</h1>
            </Row>
            <Spin spinning={loadingRoles}>
                <Row gutter={16} style={{ marginTop: '2%' }}>
                    {roles.map(role => (
                        <Col key={role.id} span={24}>
                            <RoleCard
                                id={role.id}
                                name={role.name}
                                type={role.type}
                                childRoles={role.childRoles}
                                navigate={navigate}
                                disableDelete={true}
                            />
                        </Col>
                    ))}
                </Row>
            </Spin>
        </div>
    );
}

export default Roles;