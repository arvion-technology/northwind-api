// Northwind API explorer. The C# API is the real implementation; this page is a
// faithful browser mirror of its validation, pagination and problem-details
// rules so the demo runs with no server. No HTTP request leaves this page.
const $ = A.$, $$ = A.$$, el = A.el;

/* ---------------------------------------------------------------- seed data
   Mirrors src/Northwind.Api/Repositories/SeedData.cs: fixed ids and dates. */

const CUSTOMERS = [
  ["000101", "Aarav Sharma", "aarav.sharma@example.com", "Nepal", "enterprise", "2026-01-08T09:30:00Z"],
  ["000102", "Mira Karki", "mira.karki@example.com", "Nepal", "growth", "2026-02-14T09:30:00Z"],
  ["000103", "Daniel Okafor", "daniel.okafor@example.com", "Nigeria", "growth", "2026-03-02T09:30:00Z"],
  ["000104", "Sofia Rossi", "sofia.rossi@example.com", "Italy", "free", "2026-03-21T09:30:00Z"],
  ["000105", "Kenji Watanabe", "kenji.watanabe@example.com", "Japan", "enterprise", "2026-04-09T09:30:00Z"],
  ["000106", "Priya Nair", "priya.nair@example.com", "India", "growth", "2026-05-04T09:30:00Z"],
  ["000107", "Lucas Meyer", "lucas.meyer@example.com", "Germany", "free", "2026-05-26T09:30:00Z"],
  ["000108", "Amara Diallo", "amara.diallo@example.com", "Senegal", "growth", "2026-06-17T09:30:00Z"],
  ["000109", "Nora Jensen", "nora.jensen@example.com", "Denmark", "enterprise", "2026-07-01T09:30:00Z"],
  ["000110", "Rafael Costa", "rafael.costa@example.com", "Brazil", "free", "2026-08-12T09:30:00Z"],
  ["000111", "Hana Kim", "hana.kim@example.com", "South Korea", "growth", "2026-09-03T09:30:00Z"],
  ["000112", "Elias Haddad", "elias.haddad@example.com", "Lebanon", "free", "2026-10-06T09:30:00Z"],
].map(([suffix, name, email, country, tier, createdAt]) => ({
  id: guid(suffix), name, email, country, tier, createdAt,
}));

const PRODUCTS = [
  ["000201", "Khukuri chef knife", 89.0],
  ["000202", "Pashmina throw", 64.5],
  ["000203", "Cast iron skillet", 42.0],
  ["000204", "Himalayan tea sampler", 28.75],
  ["000205", "Leather field journal", 36.0],
  ["000206", "Copper water bottle", 51.25],
  ["000207", "Wool hiking socks", 19.9],
  ["000208", "Bamboo desk tray", 24.0],
].map(([suffix, name, price]) => ({ id: guid(suffix), name, price }));

const STATUSES = ["delivered", "delivered", "shipped", "paid", "pending", "delivered", "cancelled", "paid"];
const ORDERS = Array.from({ length: 24 }, (_, index) => {
  const customer = CUSTOMERS[index % CUSTOMERS.length];
  const first = PRODUCTS[index % PRODUCTS.length];
  const second = PRODUCTS[(index + 3) % PRODUCTS.length];
  const lines = [
    line(first, 1 + (index % 3)),
    line(second, 1),
  ];
  if (index % 4 === 0) lines.push(line(PRODUCTS[(index + 5) % PRODUCTS.length], 2));
  return {
    id: guid(`0003${String(index + 1).padStart(2, "0")}`),
    customerId: customer.id,
    lines,
    status: STATUSES[index % STATUSES.length],
    placedAt: `${String(2026).padStart(4, "0")}-${String(1 + (index % 9)).padStart(2, "0")}-${String(1 + (index % 27)).padStart(2, "0")}T09:30:00Z`,
    total: round(lines.reduce((sum, entry) => sum + entry.lineTotal, 0)),
  };
});

function guid(suffix) {
  return `00000000-0000-0000-0000-${String(suffix).padStart(12, "0")}`;
}
function line(product, quantity) {
  const unitPrice = product.price;
  return { productId: product.id, productName: product.name, quantity, unitPrice, lineTotal: round(quantity * unitPrice) };
}
function round(value) {
  return Math.round(value * 100) / 100;
}

// Two mutable stores so POST and cancel change subsequent responses, like the API.
const customers = CUSTOMERS.map((customer) => ({ ...customer }));
const orders = ORDERS.map((order) => ({ ...order, lines: order.lines.map((entry) => ({ ...entry })) }));

export const TEST_COUNT = 38;

/* ---------------------------------------------------------------- problem details */

function problem(status, title, detail, type, errors) {
  return {
    status,
    contentType: "application/problem+json",
    body: {
      type: type || `https://httpstatuses.io/${status}`,
      title,
      status,
      detail,
      ...(errors ? { errors } : {}),
    },
  };
}

function validationProblem(errors) {
  return problem(400, "Validation failed", "The request body or query string failed validation.", "https://httpstatuses.io/400", errors);
}

function notFound(detail) {
  return problem(404, "Resource not found", detail);
}

function conflict(detail) {
  return problem(409, "Request conflicts with the current state", detail);
}

function ok(body, status = 200, contentType = "application/json") {
  return { status, contentType, body };
}

/* ---------------------------------------------------------------- mirror rules */

const Tiers = ["free", "growth", "enterprise"];
const CustomerSorts = ["name", "country", "tier", "createdAt"];
const OrderSorts = ["placedAt", "total", "status"];
const EmailPattern = /^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$/;

function validatePaging(page, pageSize) {
  const errors = {};
  if (!Number.isInteger(page) || page < 1) errors.page = ["Page must be 1 or greater."];
  if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > 100) errors.pageSize = ["PageSize must be between 1 and 100."];
  return errors;
}

function validateQuery(sort, order, allowed) {
  const errors = {};
  if (sort && !allowed.includes(String(sort))) errors.sort = [`Sort must be one of: ${allowed.join(", ")}.`];
  if (order && !["asc", "desc"].includes(String(order))) errors.order = ["Order must be asc or desc."];
  return errors;
}

function merge(...maps) {
  return Object.assign({}, ...maps);
}

function paginate(items, page, pageSize, descending, sortKey) {
  const sorted = [...items].sort((a, b) => {
    const left = typeof sortKey(a) === "number" ? sortKey(a) : String(sortKey(a)).toLowerCase();
    const right = typeof sortKey(b) === "number" ? sortKey(b) : String(sortKey(b)).toLowerCase();
    if (left < right) return descending ? 1 : -1;
    if (left > right) return descending ? -1 : 1;
    return a.id < b.id ? -1 : 1;
  });
  const total = sorted.length;
  return {
    items: sorted.slice((page - 1) * pageSize, page * pageSize),
    page,
    pageSize,
    total,
    totalPages: total === 0 ? 0 : Math.ceil(total / pageSize),
  };
}

function parseBody(text) {
  try {
    return { ok: true, value: JSON.parse(text) };
  } catch {
    return { ok: false };
  }
}

function validateCustomer(body) {
  const errors = {};
  const name = String(body?.name || "").trim();
  const email = String(body?.email || "").trim();
  const country = String(body?.country || "").trim();
  const tier = String(body?.tier || "").trim().toLowerCase();
  if (name.length < 2) errors.name = ["Name must be at least 2 characters."];
  else if (name.length > 80) errors.name = ["Name must be 80 characters or fewer."];
  if (!EmailPattern.test(email)) errors.email = ["Email must be a valid address."];
  if (country.length < 2 || country.length > 56) errors.country = ["Country must be 2 to 56 characters."];
  if (tier && !Tiers.includes(tier)) errors.tier = ["Tier must be one of: free, growth, enterprise."];
  return errors;
}

function validateOrder(body) {
  const errors = {};
  if (!body?.customerId || body.customerId === "00000000-0000-0000-0000-000000000000") errors.customerId = ["CustomerId is required."];
  const lines = Array.isArray(body?.lines) ? body.lines : [];
  if (lines.length === 0) errors.lines = ["An order needs at least one line."];
  else if (lines.length > 20) errors.lines = ["An order can have at most 20 lines."];
  else {
    const seen = new Set();
    lines.forEach((line, index) => {
      const prefix = `lines[${index}]`;
      if (!line.productId) errors[`${prefix}.productId`] = ["ProductId is required."];
      if (!line.productName || String(line.productName).trim().length > 60) errors[`${prefix}.productName`] = ["ProductName is required, 60 characters or fewer."];
      if (!Number.isInteger(line.quantity) || line.quantity < 1 || line.quantity > 25) errors[`${prefix}.quantity`] = ["Quantity must be between 1 and 25."];
      if (typeof line.unitPrice !== "number" || line.unitPrice <= 0 || line.unitPrice > 100000) errors[`${prefix}.unitPrice`] = ["UnitPrice must be above 0 and at most 100000."];
      if (line.productId && seen.has(line.productId)) errors.lines = ["Each product may appear only once; merge duplicate lines."];
      seen.add(line.productId);
    });
  }
  return errors;
}

/* ---------------------------------------------------------------- endpoint handlers */

function listCustomers(values) {
  const page = Number(values.page), pageSize = Number(values.pageSize);
  const errors = merge(validatePaging(page, pageSize), validateQuery(values.sort, values.order, CustomerSorts));
  if (Object.keys(errors).length) return validationProblem(errors);
  const needle = String(values.search || "").trim().toLowerCase();
  const filtered = customers.filter((customer) =>
    !needle || [customer.name, customer.email, customer.country].some((field) => field.toLowerCase().includes(needle)));
  const sort = values.sort || "createdAt";
  const descending = values.order === "desc";
  const sortKey = {
    name: (customer) => customer.name,
    country: (customer) => customer.country,
    tier: (customer) => customer.tier,
    createdAt: (customer) => customer.createdAt,
  }[sort];
  return ok(paginate(filtered, page, pageSize, descending, sortKey));
}

function getCustomer(values) {
  const customer = customers.find((entry) => entry.id === values.id);
  return customer ? ok(customer) : notFound(`No customer with id ${values.id}.`);
}

function createCustomer(values) {
  const parsed = parseBody(values.body);
  if (!parsed.ok) return problem(400, "Malformed JSON", "Request body is not valid JSON.");
  const errors = validateCustomer(parsed.value);
  if (Object.keys(errors).length) return validationProblem(errors);
  const email = parsed.value.email.trim().toLowerCase();
  if (customers.some((customer) => customer.email === email)) return conflict(`A customer with email ${email} already exists.`);
  const customer = {
    id: cryptoRandomGuid(),
    name: parsed.value.name.trim(),
    email,
    country: parsed.value.country.trim(),
    tier: (parsed.value.tier || "free").trim().toLowerCase(),
    createdAt: new Date().toISOString(),
  };
  customers.push(customer);
  return ok(customer, 201);
}

function customerOrders(values) {
  const page = Number(values.page), pageSize = Number(values.pageSize);
  const errors = validatePaging(page, pageSize);
  if (Object.keys(errors).length) return validationProblem(errors);
  if (!customers.some((customer) => customer.id === values.id)) return notFound(`No customer with id ${values.id}.`);
  const own = orders.filter((order) => order.customerId === values.id);
  return ok(paginate(own, page, pageSize, true, (order) => order.placedAt));
}

function listOrders(values) {
  const page = Number(values.page), pageSize = Number(values.pageSize);
  const errors = merge(validatePaging(page, pageSize), validateQuery(values.sort, values.order, OrderSorts));
  const status = String(values.status || "").trim().toLowerCase();
  if (status && !STATUSES.includes(status)) errors.status = ["Status must be one of: pending, paid, shipped, delivered, cancelled."];
  if (Object.keys(errors).length) return validationProblem(errors);
  const filtered = orders.filter((order) =>
    (!values.customerId || order.customerId === values.customerId) && (!status || order.status === status));
  const sort = values.sort || "placedAt";
  const descending = (values.order || "desc") === "desc";
  const sortKey = { placedAt: (order) => order.placedAt, total: (order) => order.total, status: (order) => order.status }[sort];
  return ok(paginate(filtered, page, pageSize, descending, sortKey));
}

function getOrder(values) {
  const order = orders.find((entry) => entry.id === values.id);
  return order ? ok(order) : notFound(`No order with id ${values.id}.`);
}

function createOrder(values) {
  const parsed = parseBody(values.body);
  if (!parsed.ok) return problem(400, "Malformed JSON", "Request body is not valid JSON.");
  const errors = validateOrder(parsed.value);
  if (Object.keys(errors).length) return validationProblem(errors);
  if (!customers.some((customer) => customer.id === parsed.value.customerId)) {
    return notFound(`No customer with id ${parsed.value.customerId}.`);
  }
  const lines = parsed.value.lines.map((entry) => {
    const lineTotal = round(entry.quantity * entry.unitPrice);
    return { productId: entry.productId, productName: entry.productName.trim(), quantity: entry.quantity, unitPrice: entry.unitPrice, lineTotal };
  });
  const order = {
    id: cryptoRandomGuid(),
    customerId: parsed.value.customerId,
    lines,
    status: "pending",
    placedAt: new Date().toISOString(),
    total: round(lines.reduce((sum, entry) => sum + entry.lineTotal, 0)),
  };
  orders.push(order);
  return ok(order, 201);
}

function cancelOrder(values) {
  const order = orders.find((entry) => entry.id === values.id);
  if (!order) return notFound(`No order with id ${values.id}.`);
  if (order.status !== "pending") {
    return conflict(`Order ${values.id} is ${order.status} and can no longer be cancelled. Only pending orders can be cancelled.`);
  }
  order.status = "cancelled";
  return ok(order);
}

function health() {
  return ok({ status: "ok", service: "northwind-api", version: "1.0.0" });
}

function apiInfo() {
  return ok({
    name: "Northwind API",
    version: "1.0.0",
    documentation: "/openapi/v1.json",
    endpoints: [
      "GET /api/customers", "GET /api/customers/{id}", "POST /api/customers",
      "GET /api/customers/{id}/orders", "GET /api/orders", "GET /api/orders/{id}",
      "POST /api/orders", "POST /api/orders/{id}/cancel",
    ],
  });
}

function cryptoRandomGuid() {
  const bytes = new Uint8Array(16);
  (globalThis.crypto || { getRandomValues: (array) => array.forEach?.((_, index) => { array[index] = index; }) }).getRandomValues?.(bytes);
  if (bytes.every((byte) => byte === 0)) {
    const fallback = `${Date.now()}${Math.random()}`;
    return `f${fallback.replace(/\D/g, "").padEnd(11, "0").slice(0, 11)}-0000-4000-8000-${String(orders.length).padStart(12, "0")}`;
  }
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-8${hex.slice(17, 20)}-${hex.slice(20, 32)}`;
}

/* ---------------------------------------------------------------- endpoint registry */

const ENDPOINTS = [
  {
    id: "customers-list", group: "customers", method: "GET", path: "/api/customers", name: "List customers",
    description: "Paginated customers with search, a sort whitelist and a direction. Invalid page, pageSize, sort or order returns a 400 validation problem.",
    params: [
      { name: "page", default: "1" }, { name: "pageSize", default: "2" },
      { name: "search", default: "" },
      { name: "sort", default: "createdAt", options: CustomerSorts },
      { name: "order", default: "asc", options: ["asc", "desc"] },
    ],
    send: listCustomers,
  },
  {
    id: "customer-get", group: "customers", method: "GET", path: "/api/customers/{id}", name: "Get customer",
    description: "One customer by id. An unknown id returns a 404 problem with a resource-specific detail line.",
    params: [{ name: "id", default: CUSTOMERS[0].id, path: true }],
    send: getCustomer,
  },
  {
    id: "customer-create", group: "customers", method: "POST", path: "/api/customers", name: "Create customer",
    description: "Validates name, email, country and tier. A duplicate email returns 409. A successful create returns 201 with the customer body.",
    params: [],
    body: JSON.stringify({ name: "Sita Gurung", email: "sita.gurung@example.com", country: "Nepal", tier: "growth" }, null, 2),
    send: createCustomer,
  },
  {
    id: "customer-orders", group: "customers", method: "GET", path: "/api/customers/{id}/orders", name: "Customer orders",
    description: "Orders belonging to one customer, newest first. Unknown customer returns 404 before the order query runs.",
    params: [
      { name: "id", default: CUSTOMERS[0].id, path: true },
      { name: "page", default: "1" }, { name: "pageSize", default: "3" },
    ],
    send: customerOrders,
  },
  {
    id: "orders-list", group: "orders", method: "GET", path: "/api/orders", name: "List orders",
    description: "Filter by customerId or status, then page and sort by placedAt, total or status. Status is parsed against the order state enum.",
    params: [
      { name: "page", default: "1" }, { name: "pageSize", default: "3" },
      { name: "status", default: "", options: ["", ...STATUSES] },
      { name: "customerId", default: "" },
      { name: "sort", default: "placedAt", options: OrderSorts },
      { name: "order", default: "desc", options: ["asc", "desc"] },
    ],
    send: listOrders,
  },
  {
    id: "order-get", group: "orders", method: "GET", path: "/api/orders/{id}", name: "Get order",
    description: "One order with its lines. Totals are computed from the lines, never trusted from input.",
    params: [{ name: "id", default: ORDERS[4].id, path: true }],
    send: getOrder,
  },
  {
    id: "order-create", group: "orders", method: "POST", path: "/api/orders", name: "Create order",
    description: "Checks the customer exists, validates every line, then creates a pending order. Duplicate product lines are rejected.",
    params: [],
    body: JSON.stringify({
      customerId: CUSTOMERS[0].id,
      lines: [
        { productId: PRODUCTS[0].id, productName: "Khukuri chef knife", quantity: 2, unitPrice: 89.0 },
        { productId: PRODUCTS[1].id, productName: "Pashmina throw", quantity: 1, unitPrice: 64.5 },
      ],
    }, null, 2),
    send: createOrder,
  },
  {
    id: "order-cancel", group: "orders", method: "POST", path: "/api/orders/{id}/cancel", name: "Cancel order",
    description: "Only pending orders can be cancelled. Anything paid, shipped, delivered or already cancelled returns 409.",
    params: [{ name: "id", default: ORDERS[4].id, path: true }],
    send: cancelOrder,
  },
  {
    id: "health", group: "meta", method: "GET", path: "/health", name: "Health",
    description: "Liveness probe used by the container health check.",
    params: [], send: health,
  },
  {
    id: "api-info", group: "meta", method: "GET", path: "/api", name: "Discover API",
    description: "Name, version and the route list the API actually maps.",
    params: [], send: apiInfo,
  },
];

const endpointById = (id) => ENDPOINTS.find((endpoint) => endpoint.id === id) || ENDPOINTS[0];

/* ---------------------------------------------------------------- state */

const requestValues = {};
for (const endpoint of ENDPOINTS) {
  requestValues[endpoint.id] = Object.fromEntries(endpoint.params.map((param) => [param.name, param.default]));
  if (endpoint.body) requestValues[endpoint.id].body = endpoint.body;
}
const state = { endpoint: "customers-list", response: null, tab: "body" };

/* ---------------------------------------------------------------- request view */

function buildUrl(endpoint) {
  const values = requestValues[endpoint.id];
  let path = endpoint.path;
  for (const param of endpoint.params.filter((item) => item.path)) {
    path = path.replace(`{${param.name}}`, encodeURIComponent(values[param.name] || ""));
  }
  const query = new URLSearchParams();
  for (const param of endpoint.params.filter((item) => !item.path)) {
    const value = values[param.name];
    if (value !== undefined && value !== "") query.set(param.name, value);
  }
  const suffix = query.toString();
  return suffix ? `${path}?${suffix}` : path;
}

function send(endpoint) {
  const values = requestValues[endpoint.id];
  try {
    state.response = endpoint.send(values);
  } catch (error) {
    state.response = problem(500, "Unhandled error", String(error?.message || error));
  }
  state.response.ms = 7 + ((endpoint.id.length + String(values.page || "1").length) % 21);
  state.tab = "body";
  renderResponse();
}

function paramField(param, endpoint) {
  const values = requestValues[endpoint.id];
  const id = `param-${endpoint.id}-${param.name}`;
  let input;
  if (param.options) {
    input = el("select", {
      class: "select", id, "aria-label": param.name,
      onchange: (event) => { values[param.name] = event.target.value; },
    }, ...param.options.map((option) => el("option", { value: option, text: option || "any", selected: values[param.name] === option ? "" : null })));
  } else {
    input = el("input", {
      class: "input", id, value: values[param.name], "aria-label": param.name,
      onkeydown: (event) => { if (event.key === "Enter") { event.preventDefault(); send(endpoint); } },
      oninput: (event) => { values[param.name] = event.target.value; },
    });
  }
  return el("div", { class: "param" },
    el("label", { for: id }, param.name, param.path ? el("span", { class: "required", text: "*" }) : null),
    input);
}

function renderResponse() {
  const response = state.response;
  const host = $("#response");
  if (!host) return;
  host.innerHTML = "";
  if (!response) return;

  const isOk = response.status >= 200 && response.status < 300;
  const statusText = `${response.status} ${
    response.status === 201 ? "Created"
    : response.status === 400 ? "Bad Request"
    : response.status === 404 ? "Not Found"
    : response.status === 409 ? "Conflict"
    : isOk ? "OK" : "Error"
  }`;
  const statusClass = isOk ? (response.status === 201 ? "created" : "ok") : "problem";

  const nodes = [
    el("div", { class: "resp-head" },
      el("span", { class: `status-pill ${statusClass}`, text: statusText }),
      el("span", { class: "timing", text: `${response.ms || 12} ms · simulated in the browser` }),
      el("span", { style: "flex:1" }),
      el("span", { class: "badge", text: response.contentType })),
    el("div", { class: "resp-tabs" },
      el("button", {
        class: `resp-tab ${state.tab === "body" ? "active" : ""}`, type: "button", text: "Body",
        onclick: () => { state.tab = "body"; renderResponse(); },
      }),
      el("button", {
        class: `resp-tab ${state.tab === "headers" ? "active" : ""}`, type: "button", text: "Headers",
        onclick: () => { state.tab = "headers"; renderResponse(); },
      })),
  ];

  if (response.status >= 400) {
    nodes.push(el("div", { class: "problem-callout" },
      el("b", { text: "RFC 7807 problem details" }),
      el("span", { text: ` · ${response.body.title}: ${response.body.detail}` })));
  }

  if (state.tab === "body") {
    nodes.push(el("pre", { class: "resp-body", html: highlightJson(JSON.stringify(response.body, null, 2)) }));
  } else {
    nodes.push(el("div", { class: "headers-view" },
      el("dl", { class: "kv" },
        el("dt", { text: "content-type" }), el("dd", { text: response.contentType }),
        el("dt", { text: "x-response-source" }), el("dd", { text: "browser mirror" }),
        el("dt", { text: "x-api-version" }), el("dd", { text: "1.0.0" }))));
  }

  host.append(...nodes);
}

function highlightJson(json) {
  const escaped = json.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  return escaped.replace(
    /("(?:\\.|[^"\\])*")(\s*:)?|\b(true|false|null)\b|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g,
    (match, string, colon, keyword, number) => {
      if (string && colon) return `<span class="j-key">${string}</span>${colon}`;
      if (string) return `<span class="j-str">${string}</span>`;
      if (keyword === "null") return `<span class="j-null">${keyword}</span>`;
      if (keyword) return `<span class="j-bool">${keyword}</span>`;
      if (number) return `<span class="j-num">${number}</span>`;
      return match;
    });
}

function copyText(text, message) {
  if (navigator.clipboard && window.isSecureContext) {
    navigator.clipboard.writeText(text).then(() => A.toast(message, "success")).catch(() => A.toast("Copy is blocked in this browser", "error"));
  } else {
    A.toast("Copy is blocked in this browser", "error");
  }
}

function requestPanel(endpoint) {
  const values = requestValues[endpoint.id];
  const panel = el("div", { class: "panel" },
    el("div", { class: "panel-head" }, el("span", { text: "Request" }), el("span", { style: "flex:1" }),
      el("span", { text: endpoint.method === "GET" ? "read only · no body" : "JSON body" })),
    el("div", { class: "req-bar" },
      el("span", { class: `method-pill ${endpoint.method.toLowerCase()}`, text: endpoint.method }),
      el("span", { class: "req-url", text: buildUrl(endpoint) }),
      el("button", { class: "btn primary sm", id: "send", type: "button", text: "Send", onclick: () => send(endpoint) })),
    el("div", { class: "req-body" },
      el("p", { class: "endpoint-desc", text: endpoint.description }),
      endpoint.params.length
        ? el("div", { class: "param-grid" }, ...endpoint.params.map((param) => paramField(param, endpoint)))
        : null,
      endpoint.body
        ? el("div", { class: "body-editor" },
          el("label", { for: `body-${endpoint.id}` }, "request body"),
          el("textarea", {
            id: `body-${endpoint.id}`, spellcheck: "false", "aria-label": "Request body",
            oninput: (event) => { values.body = event.target.value; },
          }, endpoint.body))
        : null,
      el("div", { class: "req-actions" },
        el("button", { class: "btn sm", type: "button", text: "Copy cURL", onclick: () => copyText(curlFor(endpoint), "cURL copied") }),
        el("button", { class: "btn sm", type: "button", text: "Reset", onclick: () => { requestValues[endpoint.id] = freshValues(endpoint); render(); } }),
        el("span", { class: "note", text: "No request leaves the page. The mirror and the C# API share these rules." }))));

  // The textarea value is set through the child array above; keep its content in sync.
  const textarea = panel.querySelector("textarea");
  if (textarea) textarea.value = values.body;

  return panel;
}

function freshValues(endpoint) {
  const values = Object.fromEntries(endpoint.params.map((param) => [param.name, param.default]));
  if (endpoint.body) values.body = endpoint.body;
  return values;
}

function curlFor(endpoint) {
  const path = buildUrl(endpoint);
  const values = requestValues[endpoint.id];
  if (endpoint.method === "GET") return `curl -s "http://localhost:5080${path}"`;
  const payload = String(values.body || "{}").replace(/'/g, "'\\''");
  return `curl -s -X POST "http://localhost:5080${path}" \\\n  -H "Content-Type: application/json" \\\n  -d '${payload}'`;
}

function explorerPage(main, endpointId) {
  const endpoint = endpointById(endpointId);
  state.endpoint = endpoint.id;
  state.response = null;

  main.append(el("section", { class: "api-hero" },
    el("div", {},
      el("div", { class: "eyebrow" }, "API explorer · .NET 8"),
      el("h1", {}, "Customers and orders, ", el("em", { text: "typed end to end." })),
      el("p", { class: "lede" }, "Records in, records out, with pagination, a sort whitelist and RFC 7807 problem details on every error path. The C# implementation is in src/Northwind.Api; this page mirrors its behaviour so you can try the routes without starting a server.")),
    el("div", { class: "api-facts" },
      el("span", {}, el("b", { text: "11" }), "routes"),
      el("span", {}, el("b", { text: String(TEST_COUNT) }), "xUnit tests"),
      el("span", {}, el("b", { text: "3" }), "problem shapes"),
      el("span", {}, el("b", { text: "0" }), "runtime deps"))));

  main.append(el("div", { class: "explorer" }, requestPanel(endpoint), el("div", { class: "panel", id: "response" })));
  send(endpoint);

  main.append(el("section", { class: "api-section" },
    el("h2", { text: "What the browser mirrors" }),
    el("p", { text: "The explorer re-implements the C# validation, paging, sorting and problem-details rules in JavaScript. It is a mirror for demonstration, not a client: there is no network call, no proxy and no hidden server in this page." }),
    el("div", { class: "mirror-grid" },
      el("div", { class: "card mirror-card" }, el("h3", { text: "Validation runs before state changes" }),
        el("p", { text: "Name lengths, email shape, country bounds, tier values, page bounds, sort whitelists and every order line are checked before the repository is touched." })),
      el("div", { class: "card mirror-card" }, el("h3", { text: "Paging and sorting are computed" }),
        el("p", { text: "TotalPages comes from the filtered set. Sort keys are whitelisted per resource, and ties break on id so the page is stable." })),
      el("div", { class: "card mirror-card" }, el("h3", { text: "Errors share one shape" }),
        el("p", { text: "Every failure is a problem object with type, title, status and detail. Validation adds an errors map keyed by field." }))),
    el("div", { class: "rule-chips" },
      ...["page >= 1", "pageSize 1..100", "sort: name,country,tier,createdAt", "status enum", "email unique", "cancel only from pending", "totals not trusted"].map((rule) => el("span", { text: rule })))));

  main.append(el("section", { class: "api-section" },
    el("h2", { text: "Run the real API" }),
    el("p", { text: "The local machine used to build this repository had no .NET SDK, so the test script falls back to the official SDK container. The same command works for the API." }),
    el("div", { class: "run-note" },
      el("div", { class: "card" },
        el("h3", { text: "Start it in Docker" }),
        el("p", { text: "Expose port 5080 and point Kestrel at 8080 inside the container." }),
        codeBlock(`docker run --rm -p 5080:8080 \\
  -v "$PWD":/app -w /app \\
  -e ASPNETCORE_URLS=http://+:8080 \\
  mcr.microsoft.com/dotnet/sdk:8.0 \\
  dotnet run --project src/Northwind.Api`)),
      el("div", { class: "card" },
        el("h3", { text: "Then ask it something" }),
        el("p", { text: "Paging, a sort whitelist, a validation failure and the OpenAPI document." }),
        codeBlock(`curl "http://localhost:5080/api/customers?page=1&pageSize=5&sort=name"
curl "http://localhost:5080/api/orders?status=pending&sort=total"
curl -X POST http://localhost:5080/api/customers \\
  -H "Content-Type: application/json" \\
  -d '{"name":"Sita","email":"sita@example.com","country":"Nepal"}'
curl http://localhost:5080/openapi/v1.json`)))));

  main.append(footer());
}

function codeBlock(code) {
  const pre = el("pre", { class: "code" });
  pre.textContent = code;
  return pre;
}

/* ---------------------------------------------------------------- problems view */

function problemsPage(main) {
  const examples = [
    {
      code: "400", title: "Validation failed", request: "GET /api/customers?page=0&pageSize=500",
      detail: "Invalid paging parameters. The errors map is keyed by the parameter that failed.",
      response: listCustomers({ page: "0", pageSize: "500", sort: "", order: "" }),
    },
    {
      code: "404", title: "Resource not found", request: "GET /api/customers/00000000-0000-0000-0000-000000000000",
      detail: "The id is well formed and the route matched, but the resource does not exist.",
      response: getCustomer({ id: "00000000-0000-0000-0000-000000000000" }),
    },
    {
      code: "409", title: "State conflict", request: "POST /api/orders/00000000-0000-0000-0000-000000000303/cancel",
      detail: "The order exists and the action is valid in general, but not from its current state.",
      response: cancelOrder({ id: ORDERS[2].id }),
    },
  ];

  main.append(el("div", { class: "crumb" }, el("a", { href: "#/", text: "Explorer" }), el("span", { text: "/" }), el("span", { text: "Problem details" })));
  main.append(el("div", { class: "api-hero" },
    el("div", {},
      el("div", { class: "eyebrow" }, "RFC 7807"),
      el("h1", {}, "One error shape, ", el("em", { text: "three statuses." })),
      el("p", { class: "lede" }, "Every failure in the API leaves through a helper that returns a problem object. AspNetCore's validation problem adds the errors map; not-found and conflict reuse the same base shape. The examples below are produced by calling the mirror handlers, not copied from screenshots."))));

  main.append(el("div", { class: "problem-grid" },
    ...examples.map((example) => el("div", { class: "card problem-card" },
      el("div", { class: "pc-head" },
        el("div", { class: "code", style: `color:${example.code === "400" ? "var(--red)" : example.code === "404" ? "var(--gold)" : "var(--blue)"}`, text: `HTTP ${example.code}` }),
        el("h3", { text: example.title }),
        el("p", { text: example.request })),
      el("pre", { html: highlightJson(JSON.stringify(example.response.body, null, 2)) }),
      el("div", { class: "pc-foot" },
        el("button", { class: "btn sm", type: "button", text: "Open in explorer",
          onclick: () => { location.hash = example.code === "400" ? "#/e/customers-list" : example.code === "404" ? "#/e/customer-get" : "#/e/order-cancel"; } }),
        el("span", { text: example.detail }))))));
  main.append(footer());
}

/* ---------------------------------------------------------------- openapi view */

const SCHEMAS = [
  { name: "Customer", props: [["id", "uuid"], ["name", "string · 2..80"], ["email", "email"], ["country", "string"], ["tier", "enum: free, growth, enterprise"], ["createdAt", "date-time"]] },
  { name: "CreateCustomer", props: [["name", "required · 2..80"], ["email", "required · email"], ["country", "required · 2..56"], ["tier", "enum · default free"]] },
  { name: "Order", props: [["id", "uuid"], ["customerId", "uuid"], ["lines", "OrderLine[]"], ["status", "enum: pending, paid, shipped, delivered, cancelled"], ["total", "number · computed"], ["placedAt", "date-time"]] },
  { name: "CreateOrder", props: [["customerId", "required · uuid"], ["lines", "required · 1..20 OrderLineInput"]] },
  { name: "PagedResult", props: [["items", "T[]"], ["page", "integer"], ["pageSize", "integer"], ["total", "integer"], ["totalPages", "integer · computed"]] },
  { name: "ValidationProblem", props: [["type", "uri"], ["title", "string"], ["status", "integer"], ["detail", "string"], ["errors", "map<string, string[]>"]] },
];

function openapiPage(main) {
  main.append(el("div", { class: "crumb" }, el("a", { href: "#/", text: "Explorer" }), el("span", { text: "/" }), el("span", { text: "OpenAPI" })));
  main.append(el("div", { class: "api-hero" },
    el("div", {},
      el("div", { class: "eyebrow" }, "OpenAPI 3.0.3"),
      el("h1", {}, "The contract ships ", el("em", { text: "with the code." })),
      el("p", { class: "lede" }, "Eight routes, eight path entries and the schemas below. The document is hand-written JSON embedded in the assembly and served at /openapi/v1.json, so it is diffable in review and adds no package dependency.")),
    el("div", { class: "api-facts" }, el("span", {}, el("b", { text: "9" }), "path entries"), el("span", {}, el("b", { text: "3.0.3" }), "OpenAPI"))));

  const paths = ENDPOINTS.map((endpoint) => ({ method: endpoint.method, path: endpoint.path, name: endpoint.name, status: endpoint.method === "POST" ? "201, 400, 404, 409" : "200, 400, 404" }));
  main.append(el("div", { class: "panel" },
    el("div", { class: "panel-head" }, el("span", { text: "Paths" })),
    A.dataTable({
      columns: [
        { key: "method", label: "Method", render: (row) => el("span", { class: `method ${row.method.toLowerCase()}`, text: row.method }) },
        { key: "path", label: "Path", render: (row) => el("code", { text: row.path }) },
        { key: "name", label: "Operation" },
        { key: "status", label: "Responses" },
      ],
      rows: paths,
    })));

  const schemaCards = SCHEMAS.map((schema) => el("div", { class: "card schema-card" },
    el("h3", { text: schema.name }),
    ...schema.props.map(([name, type]) => el("div", { class: "prop" },
      el("span", { text: name }),
      el("span", { text: type })))));

  main.append(el("section", { class: "api-section" },
    el("h2", { text: "Schemas" }),
    el("div", { class: "schema-grid" }, ...schemaCards)));

  main.append(el("div", { class: "run-note", style: "margin-top:16px" },
    el("div", { class: "card" },
      el("h3", { text: "The document on disk" }),
      el("p", { text: "src/Northwind.Api/openapi.json is embedded as a resource and served at /openapi/v1.json. There is no runtime document generator to keep in sync." }),
      codeBlock("curl http://localhost:5080/openapi/v1.json | jq '.paths | keys'")),
    el("div", { class: "card" },
      el("h3", { text: "What it does not describe" }),
      el("p", { text: "The document covers request and response shapes, query parameters and problem details. Authentication, rate limits and persistence are not part of this sample, so they are not in the contract." }))));
  main.append(footer());
}

/* ---------------------------------------------------------------- shell */

function renderSidebar() {
  const groups = { customers: $("#endpoint-customers"), orders: $("#endpoint-orders"), meta: $("#endpoint-meta") };
  for (const group of Object.values(groups)) group.innerHTML = "";
  for (const endpoint of ENDPOINTS) {
    const button = el("button", {
      class: `endpoint ${state.endpoint === endpoint.id ? "active" : ""}`,
      type: "button", "data-id": endpoint.id,
      onclick: () => { location.hash = `#/e/${endpoint.id}`; },
    },
      el("span", { class: `method ${endpoint.method.toLowerCase()}`, text: endpoint.method }),
      el("span", { class: "label", text: endpoint.name }));
    groups[endpoint.group].append(button);
  }
  $$("[data-doc]").forEach((link) => link.classList.remove("active"));
  $("#test-count").textContent = String(TEST_COUNT);
}

function footer() {
  return el("footer", { class: "api-footer" },
    el("span", { text: `Northwind API · .NET 8 minimal API · ${TEST_COUNT} xUnit tests · MIT` }),
    el("span", { class: "spacer" }),
    el("a", { href: "#/openapi", text: "OpenAPI" }),
    el("a", { href: "https://arviontech.net", target: "_blank", rel: "noopener", text: "Arvion Technologies" }),
    el("a", { href: "https://github.com/arvion-technology/northwind-api", target: "_blank", rel: "noopener", text: "Source" }));
}

function parseRoute() {
  const raw = location.hash.replace(/^#\/?/, "");
  const parts = raw.split("/").filter(Boolean);
  if (parts[0] === "e" && parts[1]) return { view: "explorer", endpoint: parts[1] };
  if (parts[0] === "problems") return { view: "problems" };
  if (parts[0] === "openapi") return { view: "openapi" };
  return { view: "explorer", endpoint: "customers-list" };
}

function render() {
  const route = parseRoute();
  const main = $("#view");
  main.innerHTML = "";
  state.endpoint = route.view === "explorer" ? route.endpoint : state.endpoint;
  renderSidebar();
  $$("[data-doc]").forEach((link) => {
    link.classList.toggle("active", (route.view === "problems" && link.dataset.doc === "problems") || (route.view === "openapi" && link.dataset.doc === "openapi"));
  });

  if (route.view === "problems") {
    $("#crumb").textContent = "Problem details";
    problemsPage(main);
  } else if (route.view === "openapi") {
    $("#crumb").textContent = "OpenAPI document";
    openapiPage(main);
  } else {
    $("#crumb").textContent = endpointById(route.endpoint).name;
    explorerPage(main, route.endpoint);
  }
  document.title = `${$("#crumb").textContent} | Northwind API`;
  window.scrollTo({ top: 0 });
}

window.addEventListener("hashchange", render);
if (!location.hash) location.hash = "#/";
render();
