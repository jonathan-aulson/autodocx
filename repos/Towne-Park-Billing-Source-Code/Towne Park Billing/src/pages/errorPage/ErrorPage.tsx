import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';

export default function ErrorPage() {
    return (
        <div className="flex flex-col items-center justify-center pt-24 pb-16 md:p-6">
            <div className="flex flex-col items-center justify-center">
                <img src='../../errorPage.svg' className="h-80 w-80 mb-8" />
                <p className="text-2xl font-bold mb-4 text-gray-500 text-center">Sorry, the page you are looking for does not exist.</p>
                <Button variant="default" className="error-button" data-qa-id="button-goToHomepage">
                    <Link to="/billing/customers">Go to Homepage</Link>
                </Button>
            </div>
        </div>
    );
}