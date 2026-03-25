const state = { tab: 'endpoints', services: [], endpoints: [] };

function activateTab(tab) {
  state.tab = tab;
  document.getElementById('tab-endpoints').classList.toggle('bg-white', tab === 'endpoints');
  document.getElementById('tab-services').classList.toggle('bg-white', tab === 'services');
  render();
  if (tab === 'services') loadServices();
  else loadEndpoints();
}

async function loadServices() {
  const res = await fetch('/admin/services');
  state.services = await res.json();
  render();
}

async function loadEndpoints() {
  const res = await fetch('/admin/endpoints');
  state.endpoints = await res.json();
  render();
}

function render() {
  const main = document.getElementById('main');
  if (!main) return;
  if (state.tab === 'services') {
    main.innerHTML = renderServices();
    return;
  }
  main.innerHTML = renderEndpoints();
}

function renderServices() {
  return `
    <div class="space-y-4">
      <div class="flex justify-between items-center">
        <h2 class="text-lg font-medium">Services</h2>
        <button class="bg-blue-600 text-white px-3 py-1 rounded" onclick="showServiceForm()">New Service</button>
      </div>
      <div class="overflow-x-auto bg-white rounded shadow">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Id</th>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Name</th>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Url</th>
              <th class="px-4 py-2 text-right text-xs font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            ${state.services.map(s => `
              <tr>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(s.id)}</td>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(s.name)}</td>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(s.url)}</td>
                <td class="px-4 py-2 text-sm text-right">
                  <button class="mr-2 px-2 py-1 bg-yellow-500 text-white rounded" onclick="showServiceForm('${encodeURIComponent(s.id)}')">Edit</button>
                  <button class="px-2 py-1 bg-red-600 text-white rounded" onclick="deleteService('${encodeURIComponent(s.id)}')">Delete</button>
                </td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;
}

function renderEndpoints() {
  return `
    <div class="space-y-4">
      <div class="flex justify-between items-center">
        <h2 class="text-lg font-medium">Endpoints</h2>
        <button class="bg-blue-600 text-white px-3 py-1 rounded" onclick="showEndpointForm()">New Endpoint</button>
      </div>
      <div class="overflow-x-auto bg-white rounded shadow">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Id</th>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Service</th>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Path</th>
              <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">Method</th>
              <th class="px-4 py-2 text-right text-xs font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            ${state.endpoints.map(e => `
              <tr>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(e.id)}</td>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(e.serviceId)}</td>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(e.path)}</td>
                <td class="px-4 py-2 text-sm text-gray-700">${escapeHtml(e.method)}</td>
                <td class="px-4 py-2 text-sm text-right">
                  <button class="mr-2 px-2 py-1 bg-yellow-500 text-white rounded" onclick="showEndpointForm('${encodeURIComponent(e.id)}')">Edit</button>
                  <button class="px-2 py-1 bg-red-600 text-white rounded" onclick="deleteEndpoint('${encodeURIComponent(e.id)}')">Delete</button>
                </td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;
}

function escapeHtml(str){
  if(!str && str !== 0) return '';
  return String(str).replace(/[&<>"'`]/g, s => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;','`':'&#96;'})[s]);
}

async function showServiceForm(id){
  let svc = { id: '', name: '', url: '' };
  if (id){
    id = decodeURIComponent(id);
    const res = await fetch('/admin/services/' + encodeURIComponent(id));
    if (res.ok) svc = await res.json();
  }
  const main = document.getElementById('main');
  main.innerHTML = `
    <div class="bg-white p-4 rounded shadow">
      <form onsubmit="event.preventDefault(); saveService(this);">
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="block text-sm font-medium text-gray-700">Id</label>
            <input name="id" class="mt-1 block w-full border-gray-300 rounded-md" value="${escapeHtml(svc.id)}" ${svc.id ? 'readonly' : ''} />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">Name</label>
            <input name="name" class="mt-1 block w-full border-gray-300 rounded-md" value="${escapeHtml(svc.name)}" />
          </div>
          <div class="md:col-span-2">
            <label class="block text-sm font-medium text-gray-700">Url</label>
            <input name="url" class="mt-1 block w-full border-gray-300 rounded-md" value="${escapeHtml(svc.url)}" />
          </div>
        </div>
        <div class="mt-4 flex items-center space-x-2">
          <button type="submit" class="bg-green-600 text-white px-4 py-2 rounded">Save</button>
          <button type="button" class="px-4 py-2 border rounded" onclick="activateTab('services')">Cancel</button>
        </div>
      </form>
    </div>
  `;
}

async function saveService(form){
  const data = { id: form.id.value.trim(), name: form.name.value.trim(), url: form.url.value.trim() };
  const method = form.id.readOnly ? 'PUT' : 'POST';
  const url = form.id.readOnly ? '/admin/services/' + encodeURIComponent(data.id) : '/admin/services';
  const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
  if (res.ok) { activateTab('services'); loadServices(); }
  else alert('Save failed: ' + (await res.text()));
}

async function deleteService(id){
  if(!confirm('Delete service?')) return;
  id = decodeURIComponent(id);
  const res = await fetch('/admin/services/' + encodeURIComponent(id), { method: 'DELETE' });
  if (res.ok) loadServices(); else alert('Delete failed');
}

async function showEndpointForm(id){
  let ep = { id: '', serviceId: '', path: '', method: 'GET' };
  if (id){
    id = decodeURIComponent(id);
    const res = await fetch('/admin/endpoints/' + encodeURIComponent(id));
    if (res.ok) ep = await res.json();
  }
  // need services for select
  const svcRes = await fetch('/admin/services');
  const services = svcRes.ok ? await svcRes.json() : [];
  const main = document.getElementById('main');
  main.innerHTML = `
    <div class="bg-white p-4 rounded shadow">
      <form onsubmit="event.preventDefault(); saveEndpoint(this);">
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="block text-sm font-medium text-gray-700">Id</label>
            <input name="id" class="mt-1 block w-full border-gray-300 rounded-md" value="${escapeHtml(ep.id)}" ${ep.id ? 'readonly' : ''} />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">Service</label>
            <select name="serviceId" class="mt-1 block w-full border-gray-300 rounded-md">
              ${services.map(s => `<option value="${escapeHtml(s.id)}" ${s.id===ep.serviceId ? 'selected' : ''}>${escapeHtml(s.name)} (${escapeHtml(s.id)})</option>`).join('')}
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">Path</label>
            <input name="path" class="mt-1 block w-full border-gray-300 rounded-md" value="${escapeHtml(ep.path)}" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">Method</label>
            <input name="method" class="mt-1 block w-full border-gray-300 rounded-md" value="${escapeHtml(ep.method || 'GET')}" />
          </div>
        </div>
        <div class="mt-4 flex items-center space-x-2">
          <button type="submit" class="bg-green-600 text-white px-4 py-2 rounded">Save</button>
          <button type="button" class="px-4 py-2 border rounded" onclick="activateTab('endpoints')">Cancel</button>
        </div>
      </form>
    </div>
  `;
}

async function saveEndpoint(form){
  const data = { id: form.id.value.trim(), serviceId: form.serviceId.value.trim(), path: form.path.value.trim(), method: form.method.value.trim() };
  const method = form.id.readOnly ? 'PUT' : 'POST';
  const url = form.id.readOnly ? '/admin/endpoints/' + encodeURIComponent(data.id) : '/admin/endpoints';
  const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
  if (res.ok) { activateTab('endpoints'); loadEndpoints(); }
  else alert('Save failed: ' + (await res.text()));
}

async function deleteEndpoint(id){
  if(!confirm('Delete endpoint?')) return;
  id = decodeURIComponent(id);
  const res = await fetch('/admin/endpoints/' + encodeURIComponent(id), { method: 'DELETE' });
  if (res.ok) loadEndpoints(); else alert('Delete failed');
}

// initialize
activateTab('endpoints');
loadEndpoints();
