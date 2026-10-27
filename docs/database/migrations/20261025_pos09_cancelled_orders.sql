
-- POS-09: Huy don hang dang ban
-- BR-POS-10: Lich su huy don
-- Issue #36

CREATE TABLE IF NOT EXISTS pos_cancelled_orders (
    id BIGINT NOT NULL AUTO_INCREMENT,
    cashier_id VARCHAR(100) NOT NULL,
    cancelled_at_utc DATETIME(6) NOT NULL,
    order_value BIGINT NOT NULL,
    item_count INT NOT NULL,
    source VARCHAR(20) NOT NULL,
    PRIMARY KEY (id)
);
