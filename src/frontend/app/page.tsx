import Link from "next/link";

export default function Home() {
  return (
    <div>
      <h1>Clinic POS Platform</h1>
      <p>Multi-tenant clinic point-of-sale system.</p>
      <Link href="/patients">Go to Patients &rarr;</Link>
    </div>
  );
}
