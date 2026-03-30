"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

type GatewayEndpoint = {
  id?: string;
  serviceId?: string;
  method?: string;
  path?: string;
};

type GatewayService = {
  id?: string;
  name?: string;
  url?: string;
  path?: string;
  endpoints?: GatewayEndpoint[];
};

type ServiceFormState = {
  id: string;
  name: string;
  url: string;
  path: string;
};

const EMPTY_FORM: ServiceFormState = {
  id: "",
  name: "",
  url: "",
  path: "",
};

function toServiceId(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

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

export default function ServicesPage() {
  const [services, setServices] = useState<GatewayService[]>([]);
  const [form, setForm] = useState<ServiceFormState>(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isEditMode = useMemo(() => form.id.trim().length > 0, [form.id]);
  const resolvedId = useMemo(() => (isEditMode ? form.id.trim() : toServiceId(form.name)), [form.id, form.name, isEditMode]);

  const fetchServices = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch("/api/admin/services", { cache: "no-store" });
      if (!response.ok) {
        throw new Error(`Failed to load services (${response.status})`);
      }

      const json = (await response.json()) as unknown;
      setServices(normalizeArray<GatewayService>(json));
    } catch (fetchError) {
      setError(fetchError instanceof Error ? fetchError.message : "Failed to load services");
      setServices([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchServices();
  }, []);

  const resetForm = () => {
    setForm(EMPTY_FORM);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setError(null);

    if (!resolvedId) {
      setError("Service name must produce a valid Id");
      setSaving(false);
      return;
    }

    const payload = {
      Id: resolvedId,
      name: form.name,
      url: form.url,
      path: form.path,
      endpoints: [],
    };

    try {
      if (isEditMode) {
        const response = await fetch(`/api/admin/services/${encodeURIComponent(form.id)}`, {
          method: "PUT",
          headers: {
            "content-type": "application/json",
          },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          throw new Error(`Failed to update service (${response.status})`);
        }
      } else {
        const response = await fetch("/api/admin/services", {
          method: "POST",
          headers: {
            "content-type": "application/json",
          },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          throw new Error(`Failed to create service (${response.status})`);
        }
      }

      resetForm();
      await fetchServices();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Failed to save service");
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (service: GatewayService) => {
    setForm({
      id: service.id ?? "",
      name: service.name ?? "",
      url: service.url ?? "",
      path: service.path ?? "",
    });
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm("Delete this service?")) {
      return;
    }

    setError(null);

    try {
      const response = await fetch(`/api/admin/services/${encodeURIComponent(id)}`, {
        method: "DELETE",
      });

      if (!response.ok) {
        throw new Error(`Failed to delete service (${response.status})`);
      }

      if (form.id === id) {
        resetForm();
      }

      await fetchServices();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Failed to delete service");
    }
  };

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <header className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Services</h1>
          <p className="text-sm text-slate-600">List and manage gateway services.</p>
        </div>
        <button
          type="button"
          onClick={() => {
            resetForm();
          }}
          className="rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          New Service
        </button>
      </header>

      {error ? (
        <div className="mb-4 rounded-md border border-rose-300 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error}</div>
      ) : null}

      <section className="mb-8 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
        <h2 className="mb-4 text-lg font-medium text-slate-900">{isEditMode ? "Edit Service" : "Create Service"}</h2>
        <form className="grid gap-4 sm:grid-cols-2" onSubmit={handleSubmit}>
          <input type="hidden" name="Id" value={resolvedId} readOnly />

          <label className="space-y-1 text-sm text-slate-700">
            <span>Name</span>
            <input
              required
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              className="w-full rounded-md border border-slate-300 px-3 py-2"
              placeholder="Orders API"
            />
          </label>

          <label className="space-y-1 text-sm text-slate-700">
            <span>Base URL</span>
            <input
              required
              value={form.url}
              onChange={(event) => setForm((current) => ({ ...current, url: event.target.value }))}
              className="w-full rounded-md border border-slate-300 px-3 py-2"
              placeholder="https://service.local"
            />
          </label>

          <label className="space-y-1 text-sm text-slate-700 sm:col-span-2">
            <span>Path Prefix</span>
            <input
              required
              value={form.path}
              onChange={(event) => setForm((current) => ({ ...current, path: event.target.value }))}
              className="w-full rounded-md border border-slate-300 px-3 py-2"
              placeholder="/orders"
            />
          </label>

          <div className="sm:col-span-2 flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={saving}
              className="rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-700 disabled:opacity-60"
            >
              {saving ? "Saving..." : isEditMode ? "Update Service" : "Create Service"}
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
          <h2 className="text-lg font-medium text-slate-900">Services List</h2>
        </div>

        {loading ? (
          <p className="px-4 py-6 text-sm text-slate-600 sm:px-6">Loading services...</p>
        ) : services.length === 0 ? (
          <p className="px-4 py-6 text-sm text-slate-600 sm:px-6">No services found.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="bg-slate-50 text-left text-slate-700">
                <tr>
                  <th className="px-4 py-3 font-semibold sm:px-6">Name</th>
                  <th className="px-4 py-3 font-semibold sm:px-6">URL</th>
                  <th className="px-4 py-3 font-semibold sm:px-6">Path</th>
                  <th className="px-4 py-3 font-semibold sm:px-6">Actions</th>
                </tr>
              </thead>
              <tbody>
                {services.map((service) => (
                  <tr key={service.id ?? `${service.name}-${service.path}`} className="border-t border-slate-200">
                    <td className="px-4 py-3 sm:px-6">{service.name ?? "-"}</td>
                    <td className="px-4 py-3 sm:px-6">{service.url ?? "-"}</td>
                    <td className="px-4 py-3 sm:px-6">{service.path ?? "-"}</td>
                    <td className="px-4 py-3 sm:px-6">
                      <div className="flex gap-3">
                        <button
                          type="button"
                          onClick={() => handleEdit(service)}
                          className="text-slate-700 hover:text-slate-900"
                        >
                          Edit
                        </button>
                        {service.id ? (
                          <button
                            type="button"
                            onClick={() => void handleDelete(service.id as string)}
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
