import { LoginPage } from '@/pages/loginPage/LoginPage';
import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';

test('renders the login page correctly', () => {
  render(<LoginPage />);

  // Check if the logo is rendered
  const logo = screen.getByAltText('Company Logo');
  expect(logo).toBeInTheDocument();
  expect(logo).toHaveAttribute('src', '/towne_park.png');

  // Check if the "Sign in with SSO" button is rendered
  const signInButton = screen.getByRole('button', { name: /sign in with Microsoft/i });
  expect(signInButton).toBeInTheDocument();
  
  // Check if the background image is rendered correctly (for large screens)
  const backgroundImage = screen.getByAltText('Background');
  expect(backgroundImage).toBeInTheDocument();
  expect(backgroundImage).toHaveAttribute('class', 'absolute inset-0 h-full w-full object-cover');
});

test('background image is set correctly', () => {
  const randomImageNumber = Math.floor(Math.random() * 3) + 1;
  jest.spyOn(Math, 'random').mockReturnValue((randomImageNumber - 1) / 3);

  render(<LoginPage />);

  const backgroundImage = screen.getByAltText('Background');
  expect(backgroundImage).toHaveAttribute('src', `/login${randomImageNumber}.png`);
  
  // Restore the original implementation
  jest.spyOn(Math, 'random').mockRestore();
});