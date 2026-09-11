import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";
import { ProblemSolution } from "./sections/problem-solution";
import { Features } from "./sections/features";
import { ClinicMap } from "./sections/clinic-map";
import { FeedPreview } from "./sections/feed-preview";
import { ForClinics } from "./sections/for-clinics";
import { Testimonials } from "./sections/testimonials";
import { CtaFooter } from "./sections/cta-footer";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
      <ProblemSolution />
      <Features />
      <ClinicMap />
      <FeedPreview />
      <ForClinics />
      <Testimonials />
      <CtaFooter />
    </>
  );
}
