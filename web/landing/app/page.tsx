import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";
import { ProblemSolution } from "./sections/problem-solution";
import { Features } from "./sections/features";
import { ClinicMap } from "./sections/clinic-map";
import { FeedPreview } from "./sections/feed-preview";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
      <ProblemSolution />
      <Features />
      <ClinicMap />
      <FeedPreview />
    </>
  );
}
