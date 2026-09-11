import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";
import { ProblemSolution } from "./sections/problem-solution";
import { Features } from "./sections/features";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
      <ProblemSolution />
      <Features />
    </>
  );
}
