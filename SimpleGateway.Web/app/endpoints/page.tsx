"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

type GatewayService = {
  id?: string;
  name?: string;
  url?: string;
  path?: string;
};

type GatewayEndpoint = {
  id?: string;
  serviceId?: string;
  method?: string;
  path?: string;
};

type EndpointFormState = {
  id: string;
  serviceId: string;
  method: string;
  path: string;
};

const EMPTY_FORM: EndpointFormState = {
  id: "",
  serviceId: "",
  method: "GET",
  path: "",
};

const HTTP_METHODS = ["GET", "POST", "PUT", "PATCH", "DELETE"];

function normalizeArray<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (payload && typeof payload === "object") {
    const record = payload as Record<string, unknown>;

    if (Array.isArray(record.items)) {
      return record.items as T[];
    }

    if (Array.isArray(record.data)) {
      return record.data as T[];
    }

    if (Array.isArray(record.result)) {
      return record.result as T[];
    }

    if (Array.isArray(record.$values)) {
      return record.$values as T[];
    }
  }

  return [];
}

export default function EndpointsPage() {
  const [endpoints, setEndpoints] = useState<GatewayEndpoint[]>([]);
  const [services, setServices] = useState<GatewayService[]>([]);
  const [form, setForm] = useState<EndpointFormState>(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isEditMode = useMemo(() => form.id.trim().length > 0, [form.id]);

  const fetchAll = async () => {
    setLoading(true);
    setError(null);

    try {
      const [endpointsResponse, servicesResponse] = await Promise.all([
        fetch("/api/admin/endpoints", { cache: "no-store" }),
        fetch("/api/admin/services", { cache: "no-store" }),
      ]);

      if (!endpointsResponse.ok) {
        throw new Error(`Failed to load endpoints (${endpointsResponse.status})`);
      }

      if (!servicesResponse.ok) {
        throw new Error(`Failed to load services (${servicesResponse.status})`);
      }

      const [endpointsJson, servicesJson] = await Promise.all([
        endpointsResponse.json() as Promise<unknown>,
        servicesResponse.json() as Promise<unknown>,
      ]);

      const nextServices = normalizeArray<GatewayService>(servicesJson);
      setServices(nextServices);

      const nextEndpoints = normalizeArray<GatewayEndpoint>(endpointsJson);
      setEndpoints(nextEndpoints);

      if (!form.serviceId && nextServices.length > 0) {
        setForm((current) => ({
          ...current,
          serviceId: current.serviceId || nextServices[0].id || "",
        }));
      }
    } catch (fetchError) {
      setError(fetchError instanceof Error ? fetchError.message : "Failed to load endpoints");
      setEndpoints([]);
      setServices([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchAll();
  }, []);

  const resetForm = () => {
    setForm(() => ({
      ...EMPTY_FORM,
      serviceId: services[0]?.id ?? "",
    }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setError(null);

    const payload = {
      Id: form.id || undefined,
      serviceId: form.serviceId,
      method: form.method,
      path: form.path,
    };

    try {
      if (isEditMode) {
        const response = await fetch(`/api/admin/endpoints/${encodeURIComponent(form.id)}`, {
          method: "PUT",
          headers: {
            "content-type": "application/json",
          },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          throw new Error(`Failed to update endpoint (${response.status})`);
        }
      } else {
        const response = await fetch("/api/admin/endpoints", {
          method: "POST",
          headers: {
            "content-type": "application/json",
          },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          throw new Error(`Failed to create endpoint (${response.status})`);
        }
      }

      resetForm();
      await fetchAll();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Failed to save endpoint");
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (endpoint: GatewayEndpoint) => {
    setForm({
      id: endpoint.id ?? "",
      serviceId: endpoint.serviceId ?? services[0]?.id ?? "",
      method: (endpoint.method ?? "GET").toUpperCase(),
      path: endpoint.path ?? "",
    });
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm("Delete this endpoint?")) {
      return;
    }

    setError(null);

    try {
      const response = await fetch(`/api/admin/endpoints/${encodeURIComponent(id)}`, {
        method: "DELETE",
      });

      if (!response.ok) {
        throw new Error(`Failed to delete endpoint (${response.status})`);
      }

      if (form.id === id) {
        resetForm();
      }

      await fetchAll();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Failed to delete endpoint");
    }
  };

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <header className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Endpoints</h1>
          <p className="text-sm text-slate-600">List and manage gateway endpoints.</p>
        </div>
        <button
          type="button"
          onClick={() => {
            resetForm();
          }}
          className="rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          New Endpoint
        </button>
      </header>

      {error ? (
        <div className="mb-4 rounded-md border border-rose-300 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
      ) : null}

      <section className="mb-8 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
        <h2 className="mb-4 text-lg font-medium text-slate-900">{isEditMode ? "Edit Endpoint" : "Create Endpoint"}</h2>
        <form className="grid gap-4 sm:grid-cols-2" onSubmit={handleSubmit}>
          <input type="hidden" name="Id" value={form.id} readOnly />

          <label className="space-y-1 text-sm text-slate-700">
            <span>Service</span>
            <select
              required
              value={form.serviceId}
              onChange={(event) => setForm((current) => ({ ...current, serviceId: event.target.value }))}
              className="w-full rounded-md border border-slate-300 px-3 py-2"
            >
              {services.map((service) => (
                <option key={service.id ?? service.name} value={service.id}>
                  {service.name ?? service.id}
                </option>
              ))}
            </select>
          </label>

          <label className="space-y-1 text-sm text-slate-700">
            <span>Method</span>
            <select
              required
              value={form.method}
              onChange={(event) => setForm((current) => ({ ...current, method: event.target.value }))}
              className="w-full rounded-md border border-slate-300 px-3 py-2"
            >
              {HTTP_METHODS.map((method) => (
                <option key={method} value={method}>
                  {method}
                </option>
              ))}
            </select>
          </label>

          <label className="space-y-1 text-sm text-slate-700 sm:col-span-2">
            <span>Path</span>
            <input
              required
              value={form.path}
              onChange={(event) => setForm((current) => ({ ...current, path: event.target.value }))}
              className="w-full rounded-md border border-slate-300 px-3 py-2"
              placeholder="/orders/{id}"
            />
          </label>

          <div className="sm:col-span-2 flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={saving || services.length === 0}
              className="rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-700 disabled:opacity-60"
            >
              {saving ? "Saving..." : isEditMode ? "Update Endpoint" : "Create Endpoint"}
            </button>
            {isEditMode ? (
              <button
                type="button"
                onClick={resetForm}
                className="rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel Edit
              </button>
            ) : null}
          </div>
        </form>
      </section>

      <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-4 py-3 sm:px-6">
          <h2 className="text-lg font-medium text-slate-900">Endpoints List</h2>
        </div>

        {loading ? (
          <p className="px-4 py-6 text-sm text-slate-600 sm:px-6">Loading endpoints...</p>
        ) : endpoints.length === 0 ? (
          <p className="px-4 py-6 text-sm text-slate-600 sm:px-6">No endpoints found.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="bg-slate-50 text-left text-slate-700">
                <tr>
                  <th className="px-4 py-3 font-semibold sm:px-6">Service ID</th>
                  <th className="px-4 py-3 font-semibold sm:px-6">Method</th>
                  <th className="px-4 py-3 font-semibold sm:px-6">Path</th>
                  <th className="px-4 py-3 font-semibold sm:px-6">Actions</th>
                </tr>
              </thead>
              <tbody>
                {endpoints.map((endpoint) => (
                  <tr key={endpoint.id ?? `${endpoint.serviceId}-${endpoint.path}-${endpoint.method}`} className="border-t border-slate-200">
                    <td className="px-4 py-3 sm:px-6">{endpoint.serviceId ?? "-"}</td>
                    <td className="px-4 py-3 sm:px-6">{endpoint.method ?? "-"}</td>
                    <td className="px-4 py-3 sm:px-6">{endpoint.path ?? "-"}</td>
                    <td className="px-4 py-3 sm:px-6">
                      <div className="flex gap-3">
                        <button
                          type="button"
                          onClick={() => handleEdit(endpoint)}
                          className="text-slate-700 hover:text-slate-900"
                        >
                          Edit
                        </button>
                        {endpoint.id ? (
                          <button
                            type="button"
                            onClick={() => void handleDelete(endpoint.id as string)}
                            className="text-rose-600 hover:text-rose-700"
                          >
                            Delete
                          </button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
