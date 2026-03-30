export default function ServicesList() {
  return (
    <div className="flex min-h-screen bg-slate-50 text-slate-900">
      <aside className="hidden w-64 border-r border-slate-200 bg-white p-6 lg:block">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
          Navigation
        </h2>
        <nav className="mt-4 space-y-2 text-sm">
          <a className="block rounded-md bg-slate-900 px-3 py-2 font-medium text-white" href="#">
            Items
          </a>
          <a className="block rounded-md px-3 py-2 font-medium text-slate-700 hover:bg-slate-100" href="#">
            Categories
          </a>
          <a className="block rounded-md px-3 py-2 font-medium text-slate-700 hover:bg-slate-100" href="#">
            Users
          </a>
        </nav>
      </aside>

      <main className="flex flex-1 flex-col">
        <header className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 bg-white px-6 py-4">
          <div>
            <h1 className="text-2xl font-semibold">CRUD Dashboard</h1>
            <p className="text-sm text-slate-600">Manage and maintain your records</p>
          </div>
          <button
            type="button"
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-700"
          >
            + Create New
          </button>
        </header>

        <section className="grid gap-4 p-6 sm:grid-cols-2 lg:grid-cols-3">
          <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-sm text-slate-500">Total Records</p>
            <p className="mt-2 text-3xl font-semibold">128</p>
          </article>
          <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-sm text-slate-500">Active Records</p>
            <p className="mt-2 text-3xl font-semibold">96</p>
          </article>
          <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-sm text-slate-500">Pending Updates</p>
            <p className="mt-2 text-3xl font-semibold">12</p>
          </article>
        </section>

        <section className="px-6 pb-6">
          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
            <table className="min-w-full text-left text-sm">
              <thead className="bg-slate-100 text-slate-700">
                <tr>
                  <th className="px-4 py-3 font-semibold">Name</th>
                  <th className="px-4 py-3 font-semibold">Status</th>
                  <th className="px-4 py-3 font-semibold">Updated</th>
                  <th className="px-4 py-3 font-semibold">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr className="border-t border-slate-200">
                  <td className="px-4 py-3">Item 001</td>
                  <td className="px-4 py-3 text-emerald-600">Active</td>
                  <td className="px-4 py-3">2026-03-30</td>
                  <td className="px-4 py-3">
                    <button type="button" className="mr-3 text-slate-700 hover:text-slate-900">
                      Edit
                    </button>
                    <button type="button" className="text-rose-600 hover:text-rose-700">
                      Delete
                    </button>
                  </td>
                </tr>
                <tr className="border-t border-slate-200">
                  <td className="px-4 py-3">Item 002</td>
                  <td className="px-4 py-3 text-amber-600">Pending</td>
                  <td className="px-4 py-3">2026-03-29</td>
                  <td className="px-4 py-3">
                    <button type="button" className="mr-3 text-slate-700 hover:text-slate-900">
                      Edit
                    </button>
                    <button type="button" className="text-rose-600 hover:text-rose-700">
                      Delete
                    </button>
                  </td>
                </tr>
                <tr className="border-t border-slate-200">
                  <td className="px-4 py-3">Item 003</td>
                  <td className="px-4 py-3 text-emerald-600">Active</td>
                  <td className="px-4 py-3">2026-03-28</td>
                  <td className="px-4 py-3">
                    <button type="button" className="mr-3 text-slate-700 hover:text-slate-900">
                      Edit
                    </button>
                    <button type="button" className="text-rose-600 hover:text-rose-700">
                      Delete
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </main>
    </div>
  );
}