from autodocx.interdeps.builder import build_interdependencies, slice_interdependencies


def test_interdependencies_build_and_slice() -> None:
    sirs = [
        {
            "name": "orders.SubmitOrder",
            "component_or_service": "orders",
            "module_name": "orders.module",
            "business_scaffold": {
                "dependencies": {"processes": ["orders.ValidateOrder"], "datastores": ["OrdersDB"]},
                "io_summary": {"identifiers": ["OrderId"]},
            },
        },
        {
            "name": "orders.ValidateOrder",
            "component_or_service": "orders",
            "module_name": "orders.module",
            "business_scaffold": {
                "dependencies": {"datastores": ["OrdersDB"]},
                "io_summary": {"identifiers": ["OrderId"]},
            },
        },
    ]
    interdeps = build_interdependencies(sirs)
    submit_node = interdeps["nodes"]["orders.SubmitOrder"]
    assert submit_node["datastores"] == ["OrdersDB"]
    assert submit_node["family"] == "orders"
    assert interdeps["modules"]["orders.module"] == ["orders.SubmitOrder", "orders.ValidateOrder"]
    slice_a = slice_interdependencies(interdeps, "orders.SubmitOrder")
    assert "orders.ValidateOrder" in slice_a["calls"]
    assert "orders.ValidateOrder" in slice_a["shared_datastores_with"]
    assert "orders.ValidateOrder" in slice_a["family_peers"]
