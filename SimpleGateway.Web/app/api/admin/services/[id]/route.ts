import { NextResponse } from "next/server";

const API_BASE_URL = process.env.API_BASE_URL;

type Params = {
  params: Promise<{ id: string }>;
};

function getApiBaseUrl(): string {
  if (!API_BASE_URL) {
    throw new Error("Missing API_BASE_URL environment variable");
  }
  return API_BASE_URL.endsWith("/") ? API_BASE_URL.slice(0, -1) : API_BASE_URL;
}

export async function GET(_: Request, { params }: Params) {
  try {
    const { id } = await params;
    const response = await fetch(`${getApiBaseUrl()}/admin/services/${encodeURIComponent(id)}`, {
      cache: "no-store",
    });

    const text = await response.text();
    return new NextResponse(text, {
      status: response.status,
      headers: {
        "content-type": response.headers.get("content-type") ?? "application/json",
      },
    });
  } catch (error) {
    return NextResponse.json(
      {
        message: error instanceof Error ? error.message : "Failed to fetch service",
      },
      { status: 500 },
    );
  }
}

export async function PUT(request: Request, { params }: Params) {
  try {
    const { id } = await params;
    const payload = await request.json();
    const response = await fetch(`${getApiBaseUrl()}/admin/services/${encodeURIComponent(id)}`, {
      method: "PUT",
      headers: {
        "content-type": "application/json",
      },
      body: JSON.stringify(payload),
    });

    const text = await response.text();
    return new NextResponse(text, {
      status: response.status,
      headers: {
        "content-type": response.headers.get("content-type") ?? "application/json",
      },
    });
  } catch (error) {
    return NextResponse.json(
      {
        message: error instanceof Error ? error.message : "Failed to update service",
      },
      { status: 500 },
    );
  }
}

export async function DELETE(_: Request, { params }: Params) {
  try {
    const { id } = await params;
    const response = await fetch(`${getApiBaseUrl()}/admin/services/${encodeURIComponent(id)}`, {
      method: "DELETE",
    });

    const text = await response.text();
    return new NextResponse(text, {
      status: response.status,
      headers: {
        "content-type": response.headers.get("content-type") ?? "application/json",
      },
    });
  } catch (error) {
    return NextResponse.json(
      {
        message: error instanceof Error ? error.message : "Failed to delete service",
      },
      { status: 500 },
    );
  }
}
