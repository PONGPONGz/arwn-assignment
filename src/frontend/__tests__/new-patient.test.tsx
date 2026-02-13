import "@testing-library/jest-dom";
import { render, screen, act } from "@testing-library/react";
import NewPatientPage from "@/app/patients/new/page";

// Mock next/navigation
jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
  }),
}));

// Mock the api module
jest.mock("@/lib/api", () => ({
  api: {
    get: jest.fn().mockResolvedValue([]),
    post: jest.fn().mockResolvedValue({}),
  },
  ApiRequestError: class extends Error {
    code: string;
    status: number;
    constructor(status: number, body: { error: { code: string; message: string } }) {
      super(body.error.message);
      this.code = body.error.code;
      this.status = status;
    }
  },
}));

describe("Create Patient Page", () => {
  it("should render form with all required fields", async () => {
    await act(async () => {
      render(<NewPatientPage />);
    });
    expect(screen.getByText("New Patient")).toBeInTheDocument();
    expect(screen.getByLabelText("First Name *")).toBeInTheDocument();
    expect(screen.getByLabelText("Last Name *")).toBeInTheDocument();
    expect(screen.getByLabelText("Phone Number *")).toBeInTheDocument();
    expect(screen.getByLabelText("Primary Branch")).toBeInTheDocument();
    expect(screen.getByText("Create Patient")).toBeInTheDocument();
  });
});
