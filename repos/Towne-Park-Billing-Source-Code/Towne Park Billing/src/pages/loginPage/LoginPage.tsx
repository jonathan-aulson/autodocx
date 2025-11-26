import { routes } from "@/authConfig";
import { ModeToggle } from "@/components/SiteHeader";
import { Button } from "@/components/ui/button";

export function LoginPage() {

  const handleLoginClick = () => {
    const redirectUi = window.location.origin;
    window.location.href = `/.auth/login/aad?post_login_redirect_uri=${redirectUi}${routes.customersList}`;
  };

  return (
    <>
      <div className="container relative h-screen flex-col items-center justify-center md:grid lg:max-w-none lg:grid-cols-2 lg:px-0">
        <div className="flex h-screen flex-col justify-center lg:p-8 relative">
          <div className="absolute top-0 right-0 p-4">
          <ModeToggle />
          </div>
          <div className="mx-auto flex w-full flex-col justify-center space-y-6 sm:w-[350px]">
            <img
              alt="Company Logo"
              className="h-20 w-20 text-center mx-auto"
              src="/towne_park.png"
              data-qa-id="image-companyLogo"
            />
            <div className="flex flex-col space-y-2 text-center">
              <h1 className="text-2xl font-semibold tracking-tight">
                Towne Park Billing
              </h1>
              <p className="text-sm text-muted-foreground">
                Please sign in with your company credentials
              </p>
            </div>
            <Button
              type="submit"
              onClick={handleLoginClick}
              data-qa-id="button-signInWithMicrosoft"
            >
              Sign in with Microsoft
            </Button>
          </div>
        </div>
        <div className="relative hidden h-screen flex-col bg-muted p-10 text-white dark:border-r lg:flex">
          <img
            alt="Background"
            className="absolute inset-0 h-full w-full object-cover"
            src={`/login${Math.floor(Math.random() * 3) + 1}.png`}
            style={{
              aspectRatio: "1027/745",
              objectFit: "cover",
            }}
            data-qa-id="image-loginBackground"
          />
        </div>
      </div>
    </>
  );
}