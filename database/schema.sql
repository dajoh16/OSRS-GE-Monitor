create extension if not exists timescaledb;

create table if not exists items_catalog (
    id bigint primary key,
    name text not null,
    metadata jsonb
);

create table if not exists price_history (
    item_id bigint not null references items_catalog(id),
    timestamp timestamptz not null,
    buy_price bigint not null,
    sell_price bigint not null,
    primary key (item_id, timestamp)
);

select create_hypertable('price_history', 'timestamp', if_not_exists => true);

create index if not exists ix_price_history_item_time_desc
    on price_history (item_id, timestamp desc);

create table if not exists alerts (
    id bigserial primary key,
    item_id bigint not null references items_catalog(id),
    timestamp timestamptz not null,
    deviation numeric(10,4) not null,
    status text not null
);

create index if not exists ix_alerts_item_status_time
    on alerts (item_id, status, timestamp desc);

create table if not exists user_positions (
    id bigserial primary key,
    item_id bigint not null references items_catalog(id),
    quantity bigint not null,
    buy_price bigint not null,
    buy_time timestamptz not null
);

create table if not exists global_configuration (
    id smallint primary key,
    standard_deviation_threshold numeric(10,4) not null
);
