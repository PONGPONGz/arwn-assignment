import "@testing-library/jest-dom";
import { render, screen, act } from "@testing-library/react";
import PatientsPage from "@/app/patients/page";

// Mock the api module
jest.mock("@/lib/api", () => ({
  api: {
    get: jest.fn().mockResolvedValue([]),
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

describe("Patients List Page", () => {
  it("should render heading and new patient link", async () => {
    await act(async () => {
      render(<PatientsPage />);
    });
    expect(screen.getByText("Patients")).toBeInTheDocument();
    expect(screen.getByText("+ New Patient")).toBeInTheDocument();
  });

  it("should render branch filter dropdown", async () => {
    await act(async () => {
      render(<PatientsPage />);
    });
    expect(screen.getByLabelText("Filter by Branch:")).toBeInTheDocument();
  });
});
